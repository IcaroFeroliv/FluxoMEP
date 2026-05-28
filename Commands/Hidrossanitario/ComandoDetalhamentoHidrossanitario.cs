using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace AirConditioningClash.Commands.Hidrossanitario
{
    using AirConditioningClash;

    [Transaction(TransactionMode.Manual)]
    public class ComandoDetalhamentoHidrossanitario : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;
            View view = doc.ActiveView;

            if (view.ViewType != ViewType.FloorPlan)
            {
                TaskDialog.Show("Erro", "Vá para uma Planta Baixa.");
                return Result.Cancelled;
            }

            // 1. CONFIGURAÇÃO E UI
            List<FamilySymbol> todasTags = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_PipeTags)
                .Cast<FamilySymbol>().ToList();

            List<FamilySymbol> tagsConexao = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_PipeFittingTags)
                .Cast<FamilySymbol>().ToList();

            if (todasTags.Count == 0) return Result.Failed;

            IList<Workset> todosWorksets = new FilteredWorksetCollector(doc)
                .OfKind(WorksetKind.UserWorkset).ToWorksets();

            Views.HID.DetalhamentoView janela = new Views.HID.DetalhamentoView(todasTags, tagsConexao, todosWorksets);
            bool? resultado = janela.ShowDialog();

            if (resultado != true) return Result.Cancelled;

            // Inputs
            bool querDiametro = janela.InserirDiametro;
            bool querInclinacao = janela.InserirInclinacao;
            bool querSentido = janela.InserirSentido;
            bool querConexao = janela.InserirConexao;
            bool usarLeader = janela.HasLeader;

            bool prefereCima = janela.PreferenciaCimaEsquerda;
            bool filtrarWorkset = janela.FiltrarPorWorkset;
            string nomeWorksetAlvo = janela.NomeWorksetEscolhido;
            bool usarSelecaoManual = janela.UsarSelecaoManual;

            ElementId idSymDiametro = janela.IdTagDiametroSelecionada;
            ElementId idSymInclinacao = janela.IdTagInclinacaoSelecionada;
            ElementId idSymSentidoSelecionado = janela.IdTagSentidoSelecionada;
            ElementId idSymConexao = janela.IdTagConexaoSelecionada; 

            // Famílias Direita/Esquerda
            FamilySymbol symSentidoDireita = null;
            FamilySymbol symSentidoEsquerda = null;

            if (querSentido && idSymSentidoSelecionado != null)
            {
                FamilySymbol symSelecionado = doc.GetElement(idSymSentidoSelecionado) as FamilySymbol;
                string nomeSel = symSelecionado.Name;
                string familiaSel = symSelecionado.FamilyName;

                string nomeBusca = "";
                if (nomeSel.IndexOf("Direita", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    symSentidoDireita = symSelecionado;
                    nomeBusca = nomeSel.Replace("Direita", "Esquerda");
                }
                else if (nomeSel.IndexOf("Esquerda", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    symSentidoEsquerda = symSelecionado;
                    nomeBusca = nomeSel.Replace("Esquerda", "Direita");
                }

                if (!string.IsNullOrEmpty(nomeBusca))
                {
                    var parEncontrado = todasTags.FirstOrDefault(t => t.Name.Equals(nomeBusca, StringComparison.InvariantCultureIgnoreCase) && t.FamilyName == familiaSel);
                    if (parEncontrado != null)
                    {
                        if (symSentidoDireita == null) symSentidoDireita = parEncontrado;
                        else symSentidoEsquerda = parEncontrado;
                    }
                }
                if (symSentidoDireita == null) symSentidoDireita = symSelecionado;
                if (symSentidoEsquerda == null) symSentidoEsquerda = symSelecionado;
            }

            double conversorCmParaPes = 0.0328084;
            double distanciaOffset = janela.DistanciaCm * conversorCmParaPes;
            double compMinimoVisual = janela.ComprimentoMinimoCm * conversorCmParaPes;
            double distLongitudinal = janela.EspacamentoLongitudinalCm * conversorCmParaPes;
            bool temDeslocamento = Math.Abs(distLongitudinal) > 0.001;

            using (Transaction t = new Transaction(doc, "Ativar Tags"))
            {
                t.Start();
                if (idSymDiametro != null) { var s = doc.GetElement(idSymDiametro) as FamilySymbol; if (!s.IsActive) s.Activate(); }
                if (idSymInclinacao != null) { var s = doc.GetElement(idSymInclinacao) as FamilySymbol; if (!s.IsActive) s.Activate(); }
                if (idSymConexao != null) { var s = doc.GetElement(idSymConexao) as FamilySymbol; if (!s.IsActive) s.Activate(); } // NOVO
                if (symSentidoDireita != null && !symSentidoDireita.IsActive) symSentidoDireita.Activate();
                if (symSentidoEsquerda != null && !symSentidoEsquerda.IsActive) symSentidoEsquerda.Activate();
                t.Commit();
            }

            // 2. COLETA (TUBOS E CONEXÕES SEPARADOS PARA NÃO QUEBRAR O SEU CÓDIGO)
            List<Pipe> tubos = new List<Pipe>();
            List<FamilyInstance> conexoes = new List<FamilyInstance>();

            if (usarSelecaoManual)
            {
                try
                {
                    IList<Reference> refs = uidoc.Selection.PickObjects(ObjectType.Element, new ElementoHidroFilter(), "Selecione tubos e conexões (ESC p/ cancelar)");
                    foreach (Reference r in refs)
                    {
                        Element e = doc.GetElement(r);
                        if (e is Pipe p) tubos.Add(p);
                        else if (e is FamilyInstance fi && fi.Category.Id.Value == (int)BuiltInCategory.OST_PipeFitting)
                        {
                            // Filtro Anti-Duplicação para evitar várias tags na mesma conexão
                            if (fi.SuperComponent == null) conexoes.Add(fi);
                        }
                    }
                }
                catch (Autodesk.Revit.Exceptions.OperationCanceledException) { return Result.Cancelled; }
            }
            else
            {
                var colPipes = new FilteredElementCollector(doc, view.Id).OfCategory(BuiltInCategory.OST_PipeCurves).WhereElementIsNotElementType().Cast<Pipe>();
                var colFittings = new FilteredElementCollector(doc, view.Id).OfCategory(BuiltInCategory.OST_PipeFitting).WhereElementIsNotElementType().Cast<FamilyInstance>();

                if (filtrarWorkset && !string.IsNullOrEmpty(nomeWorksetAlvo))
                {
                    colPipes = colPipes.Where(t =>
                    {
                        Workset w = doc.GetWorksetTable().GetWorkset(t.WorksetId);
                        return w != null && w.Name.Equals(nomeWorksetAlvo, StringComparison.InvariantCultureIgnoreCase);
                    });

                    colFittings = colFittings.Where(t =>
                    {
                        Workset w = doc.GetWorksetTable().GetWorkset(t.WorksetId);
                        return w != null && w.Name.Equals(nomeWorksetAlvo, StringComparison.InvariantCultureIgnoreCase);
                    });
                }

                tubos = colPipes.ToList();
                // Filtro Anti-Duplicação
                conexoes = colFittings.Where(fi => fi.SuperComponent == null).ToList();
            }

            tubos = tubos.OrderByDescending(t => t.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH).AsDouble()).ToList();

            if (tubos.Count == 0 && conexoes.Count == 0) return Result.Succeeded;

            // 3. PROCESSAMENTO
            List<XYZ> locaisOcupados = new List<XYZ>();
            int criadas = 0;

            using (Transaction t = new Transaction(doc, "Inserir Tags"))
            {
                t.Start();

                // =========================================================================
                // LOOP 1: TUBOS (100% IDÊNTICO AO SEU CÓDIGO ORIGINAL)
                // =========================================================================
                foreach (Pipe tubo in tubos)
                {
                    LocationCurve locCurve = tubo.Location as LocationCurve;
                    if (locCurve == null) continue;
                    Line linha = locCurve.Curve as Line;
                    if (linha == null) continue;

                    XYZ p1 = linha.GetEndPoint(0);
                    XYZ p2 = linha.GetEndPoint(1);

                    // --- DETECÇÃO DE VENTILAÇÃO ---
                    bool ehVentilacao = false;
                    try
                    {
                        Parameter pSysType = tubo.get_Parameter(BuiltInParameter.RBS_PIPING_SYSTEM_TYPE_PARAM);
                        if (pSysType != null && pSysType.AsElementId() != ElementId.InvalidElementId)
                        {
                            PipingSystemType sysType = doc.GetElement(pSysType.AsElementId()) as PipingSystemType;
                            if (sysType != null)
                            {
                                if (sysType.SystemClassification == MEPSystemClassification.Vent) ehVentilacao = true;
                                if (!ehVentilacao && CultureInfo.InvariantCulture.CompareInfo.IndexOf(sysType.Name, "ventil", CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) >= 0) ehVentilacao = true;
                            }
                        }
                    }
                    catch { }

                    if (!ehVentilacao)
                    {
                        Workset wSet = doc.GetWorksetTable().GetWorkset(tubo.WorksetId);
                        string nomeW = wSet != null ? wSet.Name : "";
                        if (CultureInfo.InvariantCulture.CompareInfo.IndexOf(nomeW, "ventil", CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) >= 0) ehVentilacao = true;
                    }

                    // --- CÁLCULO DE FLUXO ---
                    bool tuboPlano = Math.Abs(p1.Z - p2.Z) < 0.0065;
                    XYZ vetorFluxoReal = p2 - p1;

                    if (!tuboPlano)
                    {
                        if (p1.Z > p2.Z) vetorFluxoReal = ehVentilacao ? p1 - p2 : p2 - p1;
                        else vetorFluxoReal = ehVentilacao ? p2 - p1 : p1 - p2;
                    }

                    XYZ vetorFluxoPlanar = new XYZ(vetorFluxoReal.X, vetorFluxoReal.Y, 0).Normalize();

                    // --- SELEÇÃO DE FAMÍLIA E ROTAÇÃO ---
                    FamilySymbol symbolSentidoFinal = symSentidoDireita;
                    double anguloSentidoFinal = 0;

                    if (Math.Abs(vetorFluxoPlanar.X) < 0.001) // Vertical
                    {
                        if (vetorFluxoPlanar.Y > 0) // CIMA
                        {
                            symbolSentidoFinal = symSentidoDireita;
                            anguloSentidoFinal = Math.PI / 2.0;
                        }
                        else // BAIXO
                        {
                            symbolSentidoFinal = symSentidoEsquerda;
                            anguloSentidoFinal = Math.PI / 2.0;
                        }
                    }
                    else // Horizontal
                    {
                        if (vetorFluxoPlanar.X >= 0) // Direita
                        {
                            symbolSentidoFinal = symSentidoDireita;
                            anguloSentidoFinal = Math.Atan2(vetorFluxoPlanar.Y, vetorFluxoPlanar.X);
                        }
                        else // Esquerda
                        {
                            symbolSentidoFinal = symSentidoEsquerda;
                            anguloSentidoFinal = Math.Atan2(-vetorFluxoPlanar.Y, -vetorFluxoPlanar.X);
                        }
                    }

                    // --- GEOMETRIA E INSERÇÃO INTELIGENTE ---
                    double deltaX = p2.X - p1.X;
                    double deltaY = p2.Y - p1.Y;
                    double visualLen = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);

                    if (visualLen < compMinimoVisual) continue;

                    XYZ vetorTubo = p2 - p1;
                    XYZ vetorDiretor = vetorTubo.Normalize();

                    double diametro = tubo.get_Parameter(BuiltInParameter.RBS_PIPE_OUTER_DIAMETER).AsDouble();
                    double raio = diametro / 2.0;
                    double distanciaFinal = raio + distanciaOffset;

                    XYZ vetorPerp = vetorDiretor.CrossProduct(XYZ.BasisZ).Normalize();
                    if (vetorPerp.Y < -0.001 || Math.Abs(vetorPerp.Y) < 0.001 && vetorPerp.X > 0) vetorPerp = -vetorPerp;

                    double anguloTexto = Math.Atan2(vetorDiretor.Y, vetorDiretor.X);

                    double[] tentativas = { 0.5, 0.45, 0.55, 0.4, 0.6, 0.35, 0.65, 0.3, 0.7 };

                    bool encontrou = false;
                    XYZ ptDiam = null, ptInc = null, ptSentido = null;

                    bool inserirSentidoAqui = querSentido && !tuboPlano;

                    foreach (double fator in tentativas)
                    {
                        if (fator < 0.15 || fator > 0.85) continue;

                        XYZ centro = p1 + vetorTubo * fator;
                        XYZ ladoA = prefereCima ? centro + vetorPerp * distanciaFinal : centro + -vetorPerp * distanciaFinal;
                        XYZ cDiam = null, cInc = null, cSent = null;

                        if (temDeslocamento)
                        {
                            XYZ dirOffset = vetorDiretor * distLongitudinal;
                            if (querDiametro && querInclinacao && inserirSentidoAqui) { cSent = ladoA; cDiam = ladoA - dirOffset; cInc = ladoA + dirOffset; }
                            else if (querDiametro && inserirSentidoAqui) { cDiam = ladoA - dirOffset * 0.5; cSent = ladoA + dirOffset * 0.5; }
                            else if (querInclinacao && inserirSentidoAqui) { cSent = ladoA - dirOffset * 0.5; cInc = ladoA + dirOffset * 0.5; }
                            else if (inserirSentidoAqui) { cSent = ladoA + vetorDiretor * distLongitudinal; }
                            else if (querDiametro && querInclinacao) { cDiam = ladoA - dirOffset * 0.5; cInc = ladoA + dirOffset * 0.5; }
                        }
                        else
                        {
                            XYZ ladoB = prefereCima ? centro + -vetorPerp * distanciaFinal : centro + vetorPerp * distanciaFinal;
                            if (querDiametro) cDiam = ladoA;
                            if (inserirSentidoAqui) cSent = ladoA;
                            if (querInclinacao) cInc = ladoB;
                            if (querDiametro && inserirSentidoAqui) cSent = ladoA + vetorDiretor * 0.5;
                        }

                        bool okD = !querDiametro || PontoEstaLivre(doc, view.Id, cDiam, tubo.Id, locaisOcupados);
                        bool okI = !querInclinacao || PontoEstaLivre(doc, view.Id, cInc, tubo.Id, locaisOcupados);
                        bool okS = !inserirSentidoAqui || PontoEstaLivre(doc, view.Id, cSent, tubo.Id, locaisOcupados);

                        if (okD && okS && querDiametro && inserirSentidoAqui && cDiam.DistanceTo(cSent) < 0.3) okD = false;
                        if (okD && okI && querDiametro && querInclinacao && cDiam.DistanceTo(cInc) < 0.3) okD = false;
                        if (okS && okI && inserirSentidoAqui && querInclinacao && cSent.DistanceTo(cInc) < 0.3) okS = false;

                        if (okD && okI && okS)
                        {
                            ptDiam = cDiam; ptInc = cInc; ptSentido = cSent;
                            encontrou = true;
                            break;
                        }
                    }

                    if (encontrou)
                    {
                        if (querDiametro && ptDiam != null)
                        {
                            CreateTag(doc, view.Id, tubo, idSymDiametro, ptDiam, anguloTexto, usarLeader);
                            locaisOcupados.Add(ptDiam);
                            criadas++;
                        }
                        if (querInclinacao && ptInc != null)
                        {
                            CreateTag(doc, view.Id, tubo, idSymInclinacao, ptInc, anguloTexto, usarLeader);
                            locaisOcupados.Add(ptInc);
                            criadas++;
                        }
                        if (inserirSentidoAqui && ptSentido != null && symbolSentidoFinal != null)
                        {
                            CreateTag(doc, view.Id, tubo, symbolSentidoFinal.Id, ptSentido, anguloSentidoFinal, usarLeader);
                            locaisOcupados.Add(ptSentido);
                            criadas++;
                        }
                    }
                }

                // =========================================================================
                // LOOP 2: CONEXÕES (LÓGICA NOVA 100% ISOLADA)
                // =========================================================================
                if (querConexao && idSymConexao != null)
                {
                    foreach (FamilyInstance conexao in conexoes)
                    {
                        LocationPoint lp = conexao.Location as LocationPoint;
                        if (lp == null) continue;

                        XYZ[] direcoes = new XYZ[]
                        {
                            new XYZ(1, 1, 0).Normalize(),
                            new XYZ(-1, 1, 0).Normalize(),
                            new XYZ(1, -1, 0).Normalize(),
                            new XYZ(-1, -1, 0).Normalize()
                        };

                        bool inseriuConexao = false;

                        foreach (XYZ dir in direcoes)
                        {
                            XYZ ptTentativa = lp.Point + dir * distanciaOffset;
                            if (PontoEstaLivre(doc, view.Id, ptTentativa, conexao.Id, locaisOcupados))
                            {
                                CreateTag(doc, view.Id, conexao, idSymConexao, ptTentativa, 0, usarLeader);
                                criadas++;
                                locaisOcupados.Add(ptTentativa);
                                inseriuConexao = true;
                                break;
                            }
                        }

                        if (!inseriuConexao)
                        {
                            XYZ ptForcado = lp.Point + direcoes[0] * distanciaOffset;
                            CreateTag(doc, view.Id, conexao, idSymConexao, ptForcado, 0, usarLeader);
                            criadas++;
                            locaisOcupados.Add(ptForcado);
                        }
                    }
                }

                t.Commit();
            }

            TaskDialog.Show("Tags HID", $"Processo concluído!\nTotal de Tags inseridas: {criadas}");
            return Result.Succeeded;
        }

        public class ElementoHidroFilter : ISelectionFilter
        {
            public bool AllowElement(Element elem)
            {
                return elem is Pipe || (elem is FamilyInstance fi && fi.Category.Id.Value == (int)BuiltInCategory.OST_PipeFitting);
            }
            public bool AllowReference(Reference reference, XYZ position) { return false; }
        }

        // --- VALIDAÇÃO DE COLISÃO COMPLETA ---
        private bool PontoEstaLivre(Document doc, ElementId viewId, XYZ ponto, ElementId idTuboDono, List<XYZ> tagsExistentes)
        {
            if (ponto == null) return true;

            double raioProtecaoTags = 1.3;
            foreach (XYZ ocupado in tagsExistentes)
            {
                if (ponto.DistanceTo(ocupado) < raioProtecaoTags) return false;
            }

            double box = 0.35;
            XYZ min = ponto - new XYZ(box, box, box);
            XYZ max = ponto + new XYZ(box, box, box);
            Outline outline = new Outline(min, max);
            BoundingBoxIntersectsFilter filtroArea = new BoundingBoxIntersectsFilter(outline);

            List<BuiltInCategory> categoriasParaEvitar = new List<BuiltInCategory>
            {
                BuiltInCategory.OST_PipeCurves,
                BuiltInCategory.OST_PipeFitting,
                BuiltInCategory.OST_PipeAccessory,
                BuiltInCategory.OST_PipeInsulations
            };

            ElementMulticategoryFilter filtroCategorias = new ElementMulticategoryFilter(categoriasParaEvitar);
            LogicalAndFilter filtroFinal = new LogicalAndFilter(filtroArea, filtroCategorias);

            var colisoesFisicas = new FilteredElementCollector(doc, viewId)
                .WherePasses(filtroFinal)
                .ToElementIds();

            foreach (ElementId id in colisoesFisicas)
            {
                if (id != idTuboDono) return false;
            }

            var colisoesTagsAntigas = new FilteredElementCollector(doc, viewId)
                .OfCategory(BuiltInCategory.OST_PipeTags)
                .WherePasses(filtroArea)
                .GetElementCount();

            if (colisoesTagsAntigas > 0) return false;

            return true;
        }

        private void CreateTag(Document doc, ElementId viewId, Element elem, ElementId symbolId, XYZ location, double anguloDesejado, bool hasLeader)
        {
            try
            {
                // 1. SEMPRE cria a tag com 'false' primeiro. 
                // Isso impede o Revit de ignorar a nossa coordenada e aplicar a distância padrão dele.
                IndependentTag tag = IndependentTag.Create(doc, symbolId, viewId, new Reference(elem), false, TagOrientation.Horizontal, location);

                // Força a posição calculada pelo seu algoritmo
                tag.TagHeadPosition = location;

                if (hasLeader)
                {
                    // 2. Liga o Leader após a tag já estar no lugar certo
                    tag.HasLeader = true;

                    // 3. O PULO DO GATO: Define como "Livre". Isso proíbe o Revit de mover a tag!
                    tag.LeaderEndCondition = LeaderEndCondition.Free;

                    XYZ anchor;
                    if (elem.Location is LocationCurve lc)
                        anchor = lc.Curve.Project(location).XYZPoint;
                    else if (elem.Location is LocationPoint lp)
                        anchor = lp.Point;
                    else
                    {
                        var bbox = elem.get_BoundingBox(null);
                        anchor = bbox != null ? (bbox.Min + bbox.Max) / 2 : location;
                    }

                    try
                    {
                        tag.SetLeaderEnd(new Reference(elem), anchor);
                        // Reafirma a posição da cabeça da tag para garantir que o Leader não a puxou
                        tag.TagHeadPosition = location;
                    }
                    catch { }
                }

                // Rotaciona a tag se necessário
                Line eixoRotacao = Line.CreateBound(location, location + XYZ.BasisZ);
                if (Math.Abs(anguloDesejado) > 0.001)
                {
                    ElementTransformUtils.RotateElement(doc, tag.Id, eixoRotacao, anguloDesejado);
                }
            }
            catch { }
        }
    }
}