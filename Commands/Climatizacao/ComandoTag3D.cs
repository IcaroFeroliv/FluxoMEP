using AirConditioningClash.Utils;
using AirConditioningClash.Views;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using View = Autodesk.Revit.DB.View;


namespace AirConditioningClash.Commands.Climatizacao
{
    [Transaction(TransactionMode.Manual)]
    public class ComandoTag3D : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            // 1. Valida que a vista ativa é uma vista 3D ortográfica ou corte
            View activeView = doc.ActiveView;
            bool is3D    = activeView is View3D v3d && !v3d.IsPerspective;
            bool isCorte = activeView is ViewSection vs &&
                           (vs.ViewType == ViewType.Section || vs.ViewType == ViewType.Elevation);

            if (!is3D && !isCorte)
            {
                TaskDialog.Show("Tags", "Ative uma vista 3D ortográfica ou de corte antes de usar esta ferramenta.");
                return Result.Cancelled;
            }

            try
            {
                // 2. Configuração via UI
                var configView = new AirConditioningClash.Views.Climatizacao.Tag3DConfigView(doc, activeView);
                if (configView.ShowDialog() != true) return Result.Cancelled;
                Tag3DSettings settings = configView.Settings;

                // 3. Busca a família de tag selecionada
                FamilySymbol tagSymbol = new FilteredElementCollector(doc)
                    .OfClass(typeof(FamilySymbol))
                    .Cast<FamilySymbol>()
                    .FirstOrDefault(x => x.Name == settings.LastFamilySymbolName);

                if (tagSymbol == null)
                {
                    TaskDialog.Show("Erro", "Família de tag não encontrada no projeto.");
                    return Result.Failed;
                }

                // 4. Coleta elementos visíveis na vista ativa
                double minLengthInternal = UnitUtils.ConvertToInternalUnits(settings.MinimumLengthMm, UnitTypeId.Millimeters);
                double offsetVInternal   = UnitUtils.ConvertToInternalUnits(settings.OffsetMm, UnitTypeId.Millimeters);
                double offsetHInternal   = UnitUtils.ConvertToInternalUnits(settings.OffsetHorizontalMm, UnitTypeId.Millimeters);

                List<Element> toTag = ColetarElementos(doc, activeView, settings, minLengthInternal);

                if (toTag.Count == 0)
                {
                    TaskDialog.Show("Tags", "Nenhum elemento visível encontrado na vista com os critérios selecionados.");
                    return Result.Cancelled;
                }

                // 5. Insere as tags
                int criadas = 0;
                int erros   = 0;

                using (Transaction t = new Transaction(doc, "Inserir Tags 3D / Corte"))
                {
                    t.Start();

                    if (!tagSymbol.IsActive)
                        tagSymbol.Activate();

                    foreach (Element elem in toTag)
                    {
                        try
                        {
                            XYZ headPos = CalcularPosicaoTag(elem, activeView, settings, offsetVInternal, offsetHInternal);
                            if (headPos == null) continue;

                            InserirTag(doc, activeView, tagSymbol, elem, headPos, settings.HasLeader);
                            criadas++;
                        }
                        catch
                        {
                            erros++;
                        }
                    }

                    t.Commit();
                }

                string resumo = $"Tags criadas: {criadas}";
                if (erros > 0) resumo += $"\nErros ignorados: {erros}";
                TaskDialog.Show("Tags", resumo);

                return Result.Succeeded;
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return Result.Cancelled;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }

        private List<Element> ColetarElementos(Document doc, View view, Tag3DSettings settings, double minLengthInternal)
        {
            var resultado = new List<Element>();
            var viewCollector = new FilteredElementCollector(doc, view.Id);
            bool isCorte = view.ViewType == ViewType.Section || view.ViewType == ViewType.Elevation;

            // Lógica para Tubulações e Dutos
            if (settings.IsPipeCategory || settings.IsDuctCategory)
            {
                Type tipo = settings.IsPipeCategory ? typeof(Pipe) : typeof(Duct);

                foreach (MEPCurve curve in viewCollector.OfClass(tipo).Cast<MEPCurve>())
                {
                    if (curve.IsHidden(view)) continue;

                    // Filtro de tipo de tubo (só aplicado quando a lista não está vazia)
                    if (settings.IsPipeCategory && settings.TiposTuboSelecionados.Count > 0)
                    {
                        var pipeTypeName = ((Pipe)curve).PipeType?.Name;
                        if (!settings.TiposTuboSelecionados.Contains(pipeTypeName))
                            continue;
                    }

                    if (isCorte)
                    {
                        if (!GeometriaUtils.IsElementPhysicallyInSection(view, curve)) continue;
                        if (GeometriaUtils.IsOccludedByArchitecture(doc, view, curve)) continue;
                    }

                    Parameter lenParam = curve.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH);
                    if (lenParam != null && lenParam.AsDouble() < minLengthInternal) continue;

                    resultado.Add(curve);
                }
            }
            // Lógica para Equipamentos Mecânicos (Ar-Condicionado)
            else if (settings.IsEquipmentCategory)
            {
                foreach (Element equip in viewCollector
                    .OfCategory(BuiltInCategory.OST_MechanicalEquipment)
                    .WhereElementIsNotElementType())
                {
                    if (equip.IsHidden(view)) continue;

                    if (isCorte)
                    {
                        if (!GeometriaUtils.IsElementPhysicallyInSection(view, equip)) continue;
                        if (GeometriaUtils.IsOccludedByArchitecture(doc, view, equip)) continue;
                    }

                    resultado.Add(equip);
                }
            }

            return resultado;
        }

        private XYZ CalcularPosicaoTag(Element elem, View view, Tag3DSettings settings, double offsetV, double offsetH)
        {
            XYZ upDir    = view.UpDirection;
            XYZ rightDir = view.RightDirection;

            double signV = settings.TagPosicaoVertical   == "Abaixo"   ? -1.0 : 1.0;
            double signH = settings.TagPosicaoHorizontal == "Direita"  ?  1.0
                         : settings.TagPosicaoHorizontal == "Esquerda" ? -1.0 : 0.0;

            // Tubulações e dutos: referência no ponto médio da curva
            if (elem.Location is LocationCurve lc)
            {
                XYZ mid = lc.Curve.Evaluate(0.5, true);
                return mid + upDir * (signV * offsetV) + rightDir * (signH * offsetH);
            }

            // Equipamentos e demais: parte da borda do bounding box na direção escolhida
            BoundingBoxXYZ bbox = elem.get_BoundingBox(null);
            if (bbox != null)
            {
                XYZ center   = (bbox.Min + bbox.Max) / 2;
                XYZ halfDiag = (bbox.Max - bbox.Min) / 2;
                double halfV = Math.Abs(halfDiag.DotProduct(upDir));
                double halfH = Math.Abs(halfDiag.DotProduct(rightDir));

                return center
                       + upDir    * (signV * (halfV + offsetV))
                       + rightDir * (signH * (halfH + offsetH));
            }

            if (elem.Location is LocationPoint lp)
                return lp.Point + upDir * (signV * offsetV) + rightDir * (signH * offsetH);

            return null;
        }

        private void InserirTag(Document doc, View view, FamilySymbol symbol, Element elem, XYZ headPos, bool hasLeader)
        {
            Reference refElem = new Reference(elem);

            IndependentTag tag = IndependentTag.Create(
                doc,
                symbol.Id,
                view.Id,
                refElem,
                hasLeader,
                TagOrientation.Horizontal,
                headPos);

            tag.TagHeadPosition = headPos;

            if (hasLeader)
            {
                XYZ anchorPoint;
                if (elem.Location is LocationCurve lc)
                    anchorPoint = lc.Curve.Project(headPos).XYZPoint;
                else if (elem.Location is LocationPoint lp)
                    anchorPoint = lp.Point;
                else
                {
                    BoundingBoxXYZ bbox = elem.get_BoundingBox(null);
                    anchorPoint = bbox != null ? (bbox.Min + bbox.Max) / 2 : headPos;
                }

                try
                {
                    tag.SetLeaderEnd(refElem, anchorPoint);
                    tag.TagHeadPosition = headPos;
                }
                catch { }
            }
        }
    }
}
