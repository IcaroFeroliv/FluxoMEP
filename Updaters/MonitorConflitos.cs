using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing; // Necessário para Pipe
using Autodesk.Revit.UI;

namespace AirConditioningClash.Updaters
{
    public class MonitorConflitos : IUpdater
    {
        public static bool Ligado = false;
        private static AddInId _appId;
        private static UpdaterId _updaterId;

        public MonitorConflitos(AddInId id)
        {
            _appId = id;
            _updaterId = new UpdaterId(_appId, new Guid("374944E5-9118-4493-B014-5E86AA337970"));
        }

        public void Execute(UpdaterData data)
        {
            if (!Ligado) return;

            Document doc = data.GetDocument();

            // Pega os elementos modificados
            List<ElementId> idsParaChecar = new List<ElementId>();
            idsParaChecar.AddRange(data.GetModifiedElementIds());
            idsParaChecar.AddRange(data.GetAddedElementIds());

            foreach (ElementId id in idsParaChecar)
            {
                // Tenta pegar o elemento como Tubo
                Pipe tubo = doc.GetElement(id) as Pipe;
                if (tubo == null) continue;

                // --- A MÁGICA DA VERIFICAÇÃO ---
                try
                {
                    VerificarColisao(doc, tubo);
                }
                catch (Exception)
                {
                    // Silencia erros no radar para não travar o usuário
                }
            }
        }

        private void VerificarColisao(Document doc, Pipe tubo)
        {
            // 1. Pega a Geometria do Tubo (BoundingBox) no Mundo
            BoundingBoxXYZ boxTuboMundo = tubo.get_BoundingBox(null);
            if (boxTuboMundo == null) return;

            // 2. Encontrar os Vínculos (Links) carregados no projeto
            FilteredElementCollector links = new FilteredElementCollector(doc)
                .OfClass(typeof(RevitLinkInstance));

            foreach (RevitLinkInstance linkInst in links)
            {
                Document docLink = linkInst.GetLinkDocument();
                if (docLink == null) continue; // Link descarregado

                // 3. MATEMÁTICA: Transformar a Caixa do Tubo para o Espaço do Link
                // Precisamos fazer o inverso: Onde esse tubo estaria SE ele fizesse parte do arquivo linkado?
                Transform transformLink = linkInst.GetTotalTransform();
                Transform transformInversa = transformLink.Inverse;

                // Converte os cantos da caixa
                XYZ minLocal = transformInversa.OfPoint(boxTuboMundo.Min);
                XYZ maxLocal = transformInversa.OfPoint(boxTuboMundo.Max);

                // Cria uma nova caixa nas coordenadas do Link (Outline é usado para filtros)
                // Precisamos garantir que Min < Max em todos eixos
                XYZ outlineMin = new XYZ(
                    Math.Min(minLocal.X, maxLocal.X),
                    Math.Min(minLocal.Y, maxLocal.Y),
                    Math.Min(minLocal.Z, maxLocal.Z));

                XYZ outlineMax = new XYZ(
                    Math.Max(minLocal.X, maxLocal.X),
                    Math.Max(minLocal.Y, maxLocal.Y),
                    Math.Max(minLocal.Z, maxLocal.Z));

                Outline outline = new Outline(outlineMin, outlineMax);

                // 4. USAR UM FILTRO RÁPIDO (BoundingBoxIntersectsFilter)
                // Isso pergunta ao Link: "Tem algo dentro dessa caixa?"
                BoundingBoxIntersectsFilter filtroColisao = new BoundingBoxIntersectsFilter(outline);

                // 5. OBTER CATEGORIAS DA CONFIGURAÇÃO (DINÂMICO)
                List<BuiltInCategory> categoriasAtivas = AirConditioningClash.Utils.ConfiguracoesGlobais.CategoriasRadar;

                // Segurança: Se o usuário desmarcou tudo, não faz nada
                if (categoriasAtivas == null || categoriasAtivas.Count == 0) return;

                // Cria o filtro usando a lista que veio da Janela
                ElementMulticategoryFilter filtroCategorias = new ElementMulticategoryFilter(categoriasAtivas);

                // Roda a busca no Documento do Link
                var colisoes = new FilteredElementCollector(docLink)
                    .WherePasses(filtroCategorias) // <--- Usa o filtro dinâmico
                    .WherePasses(filtroColisao)
                    .ToElements();

                if (colisoes.Count > 0)
                {
                    // Achou algo! Pega o primeiro para avisar
                    Element obstaculo = colisoes.First();

                    // AVISO AO USUÁRIO
                    // Usamos TaskDialog por enquanto, mas em produto final seria melhor uma cor ou aviso na barra de status
                    TaskDialog.Show("⚡ RADAR DE COLISÃO",
                        $"Cuidado! O tubo colidiu com:\n\n" +
                        $"Elemento: {obstaculo.Name}\n" +
                        $"Categoria: {obstaculo.Category.Name}\n" +
                        $"Link: {linkInst.Name}\n\n" +
                        "Dica: Use a ferramenta 'Corrigir Conflito' para ajustar.");

                    // Para o loop assim que achar o primeiro problema (para não spamar janelas)
                    return;
                }
            }
        }

        // --- Configurações do Updater (Não mude isso) ---
        public UpdaterId GetUpdaterId() => _updaterId;
        public ChangePriority GetChangePriority() => ChangePriority.MEPFixtures;
        public string GetUpdaterName() => "Radar AirConditioning";
        public string GetAdditionalInformation() => "Monitora colisoes";
    }
}