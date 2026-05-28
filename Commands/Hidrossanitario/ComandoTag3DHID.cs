using AirConditioningClash.Utils;
using AirConditioningClash.Views.HID;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using View = Autodesk.Revit.DB.View;

namespace AirConditioningClash.Commands.Hidrossanitario
{
    [Transaction(TransactionMode.Manual)]
    public class ComandoTag3DHID : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            // 1) Valida vista ativa (3D ortográfica ou corte/elevação)
            View activeView = doc.ActiveView;
            bool is3D = activeView is View3D v3d && !v3d.IsPerspective;
            bool isCorte = activeView is ViewSection vs &&
                           (vs.ViewType == ViewType.Section || vs.ViewType == ViewType.Elevation);

            if (!is3D && !isCorte)
            {
                TaskDialog.Show("Tags HID",
                    "Ative uma vista 3D ortográfica ou de corte antes de usar esta ferramenta.");
                return Result.Cancelled;
            }

            try
            {
                // 2) Configuração via UI
                var configView = new Tag3DHIDConfigView(doc, activeView);
                if (configView.ShowDialog() != true) return Result.Cancelled;
                Tag3DHIDSettings settings = configView.Settings;

                // 3) Resolve famílias selecionadas
                FamilySymbol simDiam = ResolverFamilia(doc, settings.FamiliaDiametro);
                FamilySymbol simIncl = ResolverFamilia(doc, settings.FamiliaInclinacao);
                FamilySymbol simSent = ResolverFamilia(doc, settings.FamiliaSentido);
                FamilySymbol simConex = ResolverFamiliaConex(doc, settings.FamiliaConexao);

                if (settings.InserirDiametro && simDiam == null)
                {
                    TaskDialog.Show("Erro", "Família de tag de Diâmetro não encontrada no projeto.");
                    return Result.Failed;
                }
                if (settings.InserirInclinacao && simIncl == null)
                {
                    TaskDialog.Show("Erro", "Família de tag de Inclinação não encontrada no projeto.");
                    return Result.Failed;
                }
                if (settings.InserirSentido && simSent == null)
                {
                    TaskDialog.Show("Erro", "Família de tag de Sentido de Fluxo não encontrada no projeto.");
                    return Result.Failed;
                }
                if (settings.InserirConexao && simConex == null)
                {
                    TaskDialog.Show("Erro", "Família de tag de Conexão não encontrada no projeto.");
                    return Result.Failed;
                }

                // 4) Conversão cm → unidades internas (pés)
                double distInternal = UnitUtils.ConvertToInternalUnits(settings.DistanciaCm, UnitTypeId.Centimeters);
                double cminInternal = UnitUtils.ConvertToInternalUnits(settings.ComprimentoMinCm, UnitTypeId.Centimeters);
                double espacInternal = UnitUtils.ConvertToInternalUnits(settings.EspacamentoLongCm, UnitTypeId.Centimeters);

                // 5) Coleta elementos (manual ou automático) - AGORA COLETA TUBOS E CONEXÕES
                List<Element> elementosColetados;
                if (settings.SelecaoManual)
                {
                    elementosColetados = ColetarElementosManualmente(uidoc, settings, cminInternal, activeView);
                    if (elementosColetados == null) return Result.Cancelled; // ESC do usuário
                }
                else
                {
                    elementosColetados = ColetarElementosVisiveis(doc, activeView, settings, cminInternal);
                }

                if (elementosColetados.Count == 0)
                {
                    TaskDialog.Show("Tags HID",
                        "Nenhum elemento encontrado com os critérios selecionados.");
                    return Result.Cancelled;
                }

                // 6 e 7) Monta a lista e insere as tags em transação
                int criadas = 0, erros = 0;

                using (Transaction t = new Transaction(doc, "Inserir Tags 3D / Corte HID"))
                {
                    t.Start();

                    // Ativa as famílias base antes de usar
                    if (simDiam != null && !simDiam.IsActive) simDiam.Activate();
                    if (simIncl != null && !simIncl.IsActive) simIncl.Activate();
                    if (simSent != null && !simSent.IsActive) simSent.Activate();
                    if (simConex != null && !simConex.IsActive) simConex.Activate();

                    foreach (Element elem in elementosColetados)
                    {
                        try
                        {
                            // =========================================================================
                            // LÓGICA DE TUBOS (INTACTA CONFORME SOLICITADO)
                            // =========================================================================
                            if (elem is Pipe tubo && tubo.Location is LocationCurve lc)
                            {
                                // 1) Identifica as extremidades e direção
                                XYZ p0 = lc.Curve.GetEndPoint(0);
                                XYZ p1 = lc.Curve.GetEndPoint(1);
                                XYZ dir = (p1 - p0).Normalize();

                                bool isVertical = Math.Abs(dir.Z) > 0.95;
                                bool deveInverter = false;

                                // 2) Lógica dividida: Vertical vs Horizontal
                                if (isVertical)
                                {
                                    // Lógica para Tubos Verticais: Avalia a direção de modelagem (P0 -> P1)
                                    // Se o final (P1) é mais baixo que o início (P0), a prumada está DESCENDO.
                                    bool prumadaDescendo = p1.Z < p0.Z;
                                    deveInverter = prumadaDescendo;
                                }
                                else
                                {
                                    // Lógica para Tubos Horizontais: Avalia a gravidade visual
                                    XYZ pAlto = p0.Z > p1.Z ? p0 : p1;
                                    XYZ pBaixo = p0.Z > p1.Z ? p1 : p0;

                                    // Projeta no eixo X da tela para saber quem está na esquerda visualmente
                                    double posXAlto = pAlto.DotProduct(activeView.RightDirection);
                                    double posXBaixo = pBaixo.DotProduct(activeView.RightDirection);

                                    bool pontoAltoNaEsquerda = posXAlto < posXBaixo;

                                    if (pontoAltoNaEsquerda)
                                    {
                                        deveInverter = true;
                                    }
                                }

                                // 3) Faz a inversão da família se necessário
                                FamilySymbol simSentAtual = simSent;

                                if (deveInverter && settings.InserirSentido)
                                {
                                    string nomeOriginal = settings.FamiliaSentido;
                                    string nomeInvertido = nomeOriginal;

                                    if (nomeOriginal.Contains("Esquerda"))
                                        nomeInvertido = nomeOriginal.Replace("Esquerda", "Direita");
                                    else if (nomeOriginal.Contains("Direita"))
                                        nomeInvertido = nomeOriginal.Replace("Direita", "Esquerda");
                                    else if (nomeOriginal.Contains("esquerda"))
                                        nomeInvertido = nomeOriginal.Replace("esquerda", "direita");
                                    else if (nomeOriginal.Contains("direita"))
                                        nomeInvertido = nomeOriginal.Replace("direita", "esquerda");

                                    if (nomeInvertido != nomeOriginal)
                                    {
                                        FamilySymbol simSentInvertido = ResolverFamilia(doc, nomeInvertido);
                                        if (simSentInvertido != null)
                                        {
                                            simSentAtual = simSentInvertido;
                                            if (!simSentAtual.IsActive) simSentAtual.Activate();
                                        }
                                    }
                                }

                                // 4) MONTA A LISTA E INSERE
                                var famsPorTag = new List<FamilySymbol>();
                                if (settings.InserirDiametro) famsPorTag.Add(simDiam);
                                if (settings.InserirInclinacao && !isVertical)
                                    famsPorTag.Add(simIncl);

                                // Adiciona a tag de sentido (que pode ser a original ou a invertida)
                                if (settings.InserirSentido) famsPorTag.Add(simSentAtual);

                                // Calcula as posições geométricas e insere na vista
                                var posicoes = CalcularPosicoesTags(
                                    tubo, activeView, settings, famsPorTag.Count,
                                    distInternal, espacInternal);

                                for (int i = 0; i < famsPorTag.Count && i < posicoes.Count; i++)
                                {
                                    InserirTag(doc, activeView, famsPorTag[i], tubo, posicoes[i], settings.HasLeader);
                                    criadas++;
                                }
                            }
                            // =========================================================================
                            // NOVA LÓGICA DE CONEXÕES (PIPE FITTINGS)
                            // =========================================================================
                            else if (elem is FamilyInstance conexao && conexao.Location is LocationPoint lp)
                            {
                                if (!settings.InserirConexao || simConex == null) continue;

                                XYZ pontoInsercao = lp.Point;
                                // Para conexões, a tag fica centralizada com um deslocamento para cima
                                XYZ headPos = pontoInsercao + activeView.UpDirection * distInternal;

                                InserirTag(doc, activeView, simConex, conexao, headPos, settings.HasLeader);
                                criadas++;
                            }
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
                TaskDialog.Show("Tags HID", resumo);

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

        // ─────────────────────────────────────────────────────────────────
        // COLETA DE ELEMENTOS (Atualizado para buscar Tubos e Conexões)
        // ─────────────────────────────────────────────────────────────────

        private List<Element> ColetarElementosVisiveis(Document doc, View view, Tag3DHIDSettings settings, double cminInternal)
        {
            bool isCorte = view.ViewType == ViewType.Section || view.ViewType == ViewType.Elevation;

            // Resolve workset filtro
            WorksetId worksetIdFiltro = null;
            if (settings.FiltrarPorWorkset && doc.IsWorkshared && !string.IsNullOrEmpty(settings.WorksetSelecionado))
            {
                Workset ws = new FilteredWorksetCollector(doc)
                    .OfKind(WorksetKind.UserWorkset)
                    .FirstOrDefault(w => w.Name == settings.WorksetSelecionado);
                if (ws != null) worksetIdFiltro = ws.Id;
            }

            // Coleta Tubos e Conexões
            var todos = new FilteredElementCollector(doc, view.Id)
                .WherePasses(new LogicalOrFilter(
                    new ElementClassFilter(typeof(Pipe)),
                    new ElementCategoryFilter(BuiltInCategory.OST_PipeFitting)
                ))
                .WhereElementIsNotElementType()
                .Where(e => !e.IsHidden(view))
                .ToList();

            var resultado = new List<Element>();
            foreach (Element e in todos)
            {
                // Filtro de Workset
                if (worksetIdFiltro != null)
                {
                    var wsParam = e.get_Parameter(BuiltInParameter.ELEM_PARTITION_PARAM);
                    if (wsParam == null || wsParam.AsInteger() != worksetIdFiltro.IntegerValue)
                        continue;
                }

                // Comprimento mínimo (apenas para tubos)
                if (e is Pipe p)
                {
                    Parameter lenParam = p.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH);
                    if (lenParam != null && lenParam.AsDouble() < cminInternal) continue;
                }

                // Em corte: validar permanência no volume e oclusão por arquitetura
                if (isCorte)
                {
                    if (!GeometriaUtils.IsElementPhysicallyInSection(view, e)) continue;
                    if (GeometriaUtils.IsOccludedByArchitecture(doc, view, e)) continue;
                }

                resultado.Add(e);
            }

            return resultado;
        }

        private List<Element> ColetarElementosManualmente(UIDocument uidoc, Tag3DHIDSettings settings, double cminInternal, View view)
        {
            try
            {
                var refs = uidoc.Selection.PickObjects(
                    ObjectType.Element,
                    new ElementoHidroFilter(),
                    "Selecione os tubos e conexões para inserir as tags. Pressione Finalizar (ou ESC) ao concluir.");

                Document doc = uidoc.Document;

                WorksetId worksetIdFiltro = null;
                if (settings.FiltrarPorWorkset && doc.IsWorkshared && !string.IsNullOrEmpty(settings.WorksetSelecionado))
                {
                    Workset ws = new FilteredWorksetCollector(doc)
                        .OfKind(WorksetKind.UserWorkset)
                        .FirstOrDefault(w => w.Name == settings.WorksetSelecionado);
                    if (ws != null) worksetIdFiltro = ws.Id;
                }

                var lista = new List<Element>();
                foreach (var r in refs)
                {
                    Element e = doc.GetElement(r);
                    if (!(e is Pipe || (e is FamilyInstance fi && fi.Category.Id.Value == (int)BuiltInCategory.OST_PipeFitting)))
                        continue;

                    if (worksetIdFiltro != null)
                    {
                        var wsParam = e.get_Parameter(BuiltInParameter.ELEM_PARTITION_PARAM);
                        if (wsParam == null || wsParam.AsInteger() != worksetIdFiltro.IntegerValue)
                            continue;
                    }

                    if (e is Pipe p)
                    {
                        var lenParam = p.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH);
                        if (lenParam != null && lenParam.AsDouble() < cminInternal) continue;
                    }

                    lista.Add(e);
                }

                return lista;
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return null;
            }
        }

        private FamilySymbol ResolverFamilia(Document doc, string nome)
        {
            if (string.IsNullOrEmpty(nome)) return null;
            return new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_PipeTags)
                .Cast<FamilySymbol>()
                .FirstOrDefault(s => s.Name == nome);
        }

        // ─────────────────────────────────────────────────────────────────
        // POSICIONAMENTO DAS TAGS (Apenas para Tubos)
        // ─────────────────────────────────────────────────────────────────

        private List<XYZ> CalcularPosicoesTags(
            Pipe tubo, View view, Tag3DHIDSettings settings, int totalTags,
            double offsetPerp, double espacLong)
        {
            var posicoes = new List<XYZ>();
            if (!(tubo.Location is LocationCurve lc) || totalTags <= 0) return posicoes;

            XYZ upDir = view.UpDirection;
            XYZ rightDir = view.RightDirection;

            // Normal da vista (vetor que aponta "saindo" da tela na sua direção)
            XYZ viewNormal = rightDir.CrossProduct(upDir).Normalize();

            // Direção do tubo projetada no plano da vista
            XYZ tubeWorld = lc.Curve.GetEndPoint(1) - lc.Curve.GetEndPoint(0);
            XYZ tubeOnView = ProjetarNoPlanoDaVista(tubeWorld, rightDir, upDir);
            tubeOnView = tubeOnView.GetLength() > 1e-6 ? tubeOnView.Normalize() : rightDir;

            bool apontaParaEsquerda = tubeOnView.DotProduct(rightDir) < -1e-6;
            bool verticalParaBaixo = Math.Abs(tubeOnView.DotProduct(rightDir)) <= 1e-6 && tubeOnView.DotProduct(upDir) < -1e-6;

            if (apontaParaEsquerda || verticalParaBaixo)
            {
                tubeOnView = -tubeOnView; // Inverte o vetor
            }

            XYZ perpDir = viewNormal.CrossProduct(tubeOnView).Normalize();
            XYZ ptMid = lc.Curve.Evaluate(0.5, true);

            int sinalLado = settings.Posicao == "BaixoDireita" ? -1 : 1;

            if (totalTags == 1)
            {
                posicoes.Add(ptMid + perpDir * (sinalLado * offsetPerp));
                return posicoes;
            }

            if (espacLong > 1e-6)
            {
                for (int i = 0; i < totalTags; i++)
                {
                    double offLong = (i - (totalTags - 1) / 2.0) * espacLong;
                    XYZ basePoint = ptMid + tubeOnView * offLong;
                    posicoes.Add(basePoint + perpDir * (sinalLado * offsetPerp));
                }
                return posicoes;
            }

            for (int i = 0; i < totalTags; i++)
            {
                int sinalAlternado = (i % 2 == 0) ? sinalLado : -sinalLado;
                posicoes.Add(ptMid + perpDir * (sinalAlternado * offsetPerp));
            }
            return posicoes;
        }

        private XYZ ProjetarNoPlanoDaVista(XYZ vetor, XYZ rightDir, XYZ upDir)
        {
            double r = vetor.DotProduct(rightDir);
            double u = vetor.DotProduct(upDir);
            return rightDir * r + upDir * u;
        }

        // ─────────────────────────────────────────────────────────────────
        // INSERÇÃO DA TAG
        // ─────────────────────────────────────────────────────────────────

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
                XYZ anchor;
                if (elem.Location is LocationCurve lc)
                    anchor = lc.Curve.Project(headPos).XYZPoint;
                else if (elem.Location is LocationPoint lp)
                    anchor = lp.Point;
                else
                {
                    var bbox = elem.get_BoundingBox(null);
                    anchor = bbox != null ? (bbox.Min + bbox.Max) / 2 : headPos;
                }

                try
                {
                    tag.SetLeaderEnd(refElem, anchor);
                    tag.TagHeadPosition = headPos;
                }
                catch { }
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // SELECTION FILTERS
        // ─────────────────────────────────────────────────────────────────

        private class ElementoHidroFilter : ISelectionFilter
        {
            public bool AllowElement(Element elem) => elem is Pipe || elem is FamilyInstance fi && fi.Category.Id.Value == (int)BuiltInCategory.OST_PipeFitting;
            public bool AllowReference(Reference reference, XYZ position) => true;
        }

        private FamilySymbol ResolverFamiliaConex(Document doc, string nome)
        {
            if (string.IsNullOrEmpty(nome)) return null;
            return new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_PipeFittingTags)
                .Cast<FamilySymbol>()
                .FirstOrDefault(s => s.Name == nome);
        }
    }
}