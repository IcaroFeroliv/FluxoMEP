using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing; // Essencial para Pipe
using Autodesk.Revit.UI;

namespace AirConditioningClash.Updaters
{
    public class MonitorConflitosHID : IUpdater
    {
        // Controle de Estado Específico para HID
        public static bool Ligado = false;

        private static AddInId _appId;
        private static UpdaterId _updaterId;

        public MonitorConflitosHID(AddInId id)
        {
            _appId = id;
            // ATENÇÃO: Este GUID deve ser ÚNICO. Já gerei um novo aleatório para você aqui:
            _updaterId = new UpdaterId(_appId, new Guid("982344E1-5522-4198-A025-6F97BB448881"));
        }

        public void Execute(UpdaterData data)
        {
            // Se o botão HID não estiver ligado, aborta imediatamente
            if (!Ligado) return;

            Document doc = data.GetDocument();
            List<ElementId> idsParaChecar = new List<ElementId>();
            idsParaChecar.AddRange(data.GetModifiedElementIds());
            idsParaChecar.AddRange(data.GetAddedElementIds());

            foreach (ElementId id in idsParaChecar)
            {
                // Verifica especificamente TUBOS (Pipes)
                // Se seu plugin de Climatização usa Dutos, lá você usaria 'Duct'. Aqui usamos 'Pipe'.
                Pipe tubo = doc.GetElement(id) as Pipe;

                if (tubo == null) continue;

                try
                {
                    // Reutiliza a mesma lógica de colisão, ou você pode criar uma VerificarColisaoHID se quiser regras diferentes
                    VerificarColisao(doc, tubo);
                }
                catch (Exception)
                {
                    // Silencia erros
                }
            }
        }

        // Método de verificação (Idêntico ao original, mas dentro desta classe)
        private void VerificarColisao(Document doc, Pipe tubo)
        {
            BoundingBoxXYZ boxTuboMundo = tubo.get_BoundingBox(null);
            if (boxTuboMundo == null) return;

            FilteredElementCollector links = new FilteredElementCollector(doc).OfClass(typeof(RevitLinkInstance));

            foreach (RevitLinkInstance linkInst in links)
            {
                Document docLink = linkInst.GetLinkDocument();
                if (docLink == null) continue;

                Transform transformLink = linkInst.GetTotalTransform();
                Transform transformInversa = transformLink.Inverse;

                XYZ minLocal = transformInversa.OfPoint(boxTuboMundo.Min);
                XYZ maxLocal = transformInversa.OfPoint(boxTuboMundo.Max);

                XYZ outlineMin = new XYZ(Math.Min(minLocal.X, maxLocal.X), Math.Min(minLocal.Y, maxLocal.Y), Math.Min(minLocal.Z, maxLocal.Z));
                XYZ outlineMax = new XYZ(Math.Max(minLocal.X, maxLocal.X), Math.Max(minLocal.Y, maxLocal.Y), Math.Max(minLocal.Z, maxLocal.Z));

                Outline outline = new Outline(outlineMin, outlineMax);
                BoundingBoxIntersectsFilter filtroColisao = new BoundingBoxIntersectsFilter(outline);

                // ATENÇÃO: Lê as configurações globais compartilhadas
                List<BuiltInCategory> categoriasAtivas = AirConditioningClash.Utils.ConfiguracoesGlobais.CategoriasRadar;
                if (categoriasAtivas == null || categoriasAtivas.Count == 0) return;

                ElementMulticategoryFilter filtroCategorias = new ElementMulticategoryFilter(categoriasAtivas);

                var colisoes = new FilteredElementCollector(docLink)
                    .WherePasses(filtroCategorias)
                    .WherePasses(filtroColisao)
                    .ToElements();

                if (colisoes.Count > 0)
                {
                    Element obstaculo = colisoes.First();
                    // Personalizei o título para identificar que é o Radar HID
                    TaskDialog.Show("🚿 RADAR HIDROSSANITÁRIO",
                        $"Conflito Detectado!\n\n" +
                        $"Elemento: {obstaculo.Name}\n" +
                        $"Link: {linkInst.Name}\n");
                    return;
                }
            }
        }

        public UpdaterId GetUpdaterId() => _updaterId;
        public ChangePriority GetChangePriority() => ChangePriority.MEPFixtures;
        public string GetUpdaterName() => "Radar Hidrossanitário";
        public string GetAdditionalInformation() => "Monitora colisões de tubos";
    }
}