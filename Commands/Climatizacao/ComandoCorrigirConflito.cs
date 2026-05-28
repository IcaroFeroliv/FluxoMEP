using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using AirConditioningClash.Filters;
using AirConditioningClash.Views;
using System;
using System.Collections.Generic;

namespace AirConditioningClash.Commands.Climatizacao
{
    [Transaction(TransactionMode.Manual)]
    public class ComandoCorrigirConflito : IExternalCommand
    {

        private ISelectionFilter ObterFiltroTubulacao(TipoConflito tipo)
        {
            return new FiltroTubo(); // Sem passar tipo - sempre filtra OST_PipeCurves!
        }

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            try
            {
                // ===== ETAPA 1: SELECIONAR TIPO DE CONFLITO =====
                var janelaSelecao = new ConflictTypeSelectionView();
                janelaSelecao.ShowDialog();

                if (!janelaSelecao.Confirmado)
                    return Result.Cancelled;

                TipoConflito tipoConflito = janelaSelecao.TipoSelecionado;

                // ===== ETAPA 2: SELECIONAR OBSTÁCULO (BASEADO NO TIPO) =====
                ISelectionFilter filtroObstaculo = ObterFiltroObstaculo(tipoConflito, doc);
                string mensagemObstaculo = ObterMensagemSelecao(tipoConflito, "obstáculo");

                Reference refViga = uidoc.Selection.PickObject(
                    ObjectType.LinkedElement,
                    filtroObstaculo,
                    mensagemObstaculo);

                RevitLinkInstance linkInst = doc.GetElement(refViga.ElementId) as RevitLinkInstance;
                Document docLink = linkInst.GetLinkDocument();
                Element viga = docLink.GetElement(refViga.LinkedElementId);

                // ===== ETAPA 3: SELECIONAR TUBULAÇÃO/CONDUTO (BASEADO NO TIPO) =====
                ISelectionFilter filtroTubulacao = ObterFiltroTubulacao(tipoConflito);
                string mensagemTubulacao = ObterMensagemSelecao(tipoConflito, "tubulação");

                Reference refTubo = uidoc.Selection.PickObject(
                    ObjectType.Element,
                    filtroTubulacao,
                    mensagemTubulacao);

                Element tubo = doc.GetElement(refTubo);

                // ===== ETAPA 4: VALIDAR E CALCULAR COLISÃO =====
                if (!ValidarElementos(tubo, tipoConflito))
                {
                    TaskDialog.Show("Erro", "Elemento selecionado inválido para este tipo de conflito.");
                    return Result.Failed;
                }

                // Pega dados do obstáculo
                string nomeViga = viga.Name;
                string catViga = viga.Category.Name;

                BoundingBoxXYZ boxViga = Utils.GeometriaUtils.GetBoundingBoxNoMundo(linkInst, viga);
                double vigaTopo = boxViga.Max.Z;
                double vigaFundo = boxViga.Min.Z;

                // Pega dados da tubulação (genérico)
                string nomeElemento = tubo.Name;
                string tipoElemento = tubo.Category.Name;
                string info2 = ObterInformacaoSecundaria(tubo, tipoConflito);

                // Calcula geometria
                LocationCurve curvaElemento = tubo.Location as LocationCurve;
                if (curvaElemento == null)
                {
                    TaskDialog.Show("Erro", "Elemento selecionado não possui geometria de curva válida.");
                    return Result.Failed;
                }

                double centroCurva = curvaElemento.Curve.GetEndPoint(0).Z;

                double diametroExterno = ObterDiametro(tubo, tipoConflito);
                double espessuraIsolamento = ObterIsolamento(tubo);

                double elementoTopo = centroCurva + diametroExterno / 2 + espessuraIsolamento;
                double elementoFundo = centroCurva - diametroExterno / 2 - espessuraIsolamento;

                // Verifica colisão
                bool temColisao = elementoTopo > vigaFundo && elementoFundo < vigaTopo;

                if (!temColisao)
                {
                    TaskDialog.Show("Radar", "✅ Sem colisão detectada entre os elementos!");
                    return Result.Succeeded;
                }

                // ===== ETAPA 5: ABRIR UI DE CONFIGURAÇÃO DE DESVIO =====
                string infoViga = $"Obstáculo: {nomeViga} ({catViga})";
                string infoElemento = $"{tipoElemento}: {nomeElemento}\n{info2}";

                var janela = new Views.Climatizacao.ClashResolverView(
                    infoViga,
                    infoElemento,
                    Utils.ConfiguracoesGlobais.UltimaDirecao,
                    Utils.ConfiguracoesGlobais.UltimaMargemLateral,
                    Utils.ConfiguracoesGlobais.UltimaFolga
                );

                janela.ShowDialog();

                if (!janela.Confirmado)
                    return Result.Cancelled;

                // Salvar configurações
                Utils.ConfiguracoesGlobais.UltimaDirecao = janela.DirecaoEscolhida;
                Utils.ConfiguracoesGlobais.UltimaMargemLateral = janela.MargemLateralCm;
                Utils.ConfiguracoesGlobais.UltimaFolga = janela.FolgaCm;

                // Aplicar desvio
                AplicarDesvio(doc, tubo, curvaElemento, boxViga, centroCurva, diametroExterno,
                             espessuraIsolamento, janela, tipoConflito);

                return Result.Succeeded;
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return Result.Cancelled;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                TaskDialog.Show("Erro", "Exceção: " + ex.Message);
                return Result.Failed;
            }
        }

        private ISelectionFilter ObterFiltroObstaculo(TipoConflito tipo, Document doc)
        {
            return tipo switch
            {
                TipoConflito.Estrutural => new FiltroVigaVinculada(doc),
                TipoConflito.Eletrica => new FiltroObstaculoEletrica(doc),
                TipoConflito.Hidrossanitario => new FiltroObstaculoHidrossanitario(doc),
                _ => new FiltroVigaVinculada(doc)
            };
        }

        private string ObterMensagemSelecao(TipoConflito tipo, string elemento)
        {
            string tipoNome = tipo switch
            {
                TipoConflito.Estrutural => "Estrutural",
                TipoConflito.Eletrica => "Elétrica",
                TipoConflito.Hidrossanitario => "Hidrossanitária",
                _ => "Desconhecido"
            };

            if (elemento == "obstáculo")
                return $"Selecione o {tipoNome.ToLower()} interferente (no vínculo).";
            else
                return $"Selecione a tubulação/conduto a desviar.";
        }

        private string ObterInformacaoSecundaria(Element elem, TipoConflito tipo)
        {
            if (tipo == TipoConflito.Estrutural)
                return "Duto de Ar";

            if (tipo == TipoConflito.Eletrica)
            {
                Parameter paramDiametro = elem.get_Parameter(BuiltInParameter.RBS_CONDUIT_DIAMETER_PARAM);
                string diam = paramDiametro != null ? paramDiametro.AsValueString() : "N/A";
                return $"Diâmetro: {diam}";
            }

            // Hidrossanitário
            Parameter paramSistema = elem.get_Parameter(BuiltInParameter.RBS_PIPING_SYSTEM_TYPE_PARAM);
            string sistema = paramSistema != null ? paramSistema.AsValueString() : "Indefinido";

            Parameter paramDia = elem.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM);
            string diamPipe = paramDia != null ? paramDia.AsValueString() : "N/A";

            return $"Sistema: {sistema} | Ø {diamPipe}";
        }

        private double ObterDiametro(Element elem, TipoConflito tipo)
        {
            if (tipo == TipoConflito.Eletrica)
            {
                Parameter param = elem.get_Parameter(BuiltInParameter.RBS_CONDUIT_DIAMETER_PARAM);
                return param != null ? param.AsDouble() : 0.05;
            }

            if (tipo == TipoConflito.Hidrossanitario)
            {
                Parameter param = elem.get_Parameter(BuiltInParameter.RBS_PIPE_OUTER_DIAMETER);
                return param != null ? param.AsDouble() : 0.05;
            }

            // Estrutural (Duto) - Para dutos, retorna um tamanho padrão
            // ou tente usar o tamanho que está definido no tipo de duto
            return 0.05; // Default 5cm para dutos
        }

        private double ObterIsolamento(Element elem)
        {
            Parameter param = elem.get_Parameter(BuiltInParameter.RBS_REFERENCE_INSULATION_THICKNESS);
            return param != null ? param.AsDouble() : 0;
        }

        private bool ValidarElementos(Element elem, TipoConflito tipo)
        {
            // A ETAPA 3 SEMPRE seleciona Tubulação (PipeCurves)
            // Então validamos apenas se é uma tubulação, independente do tipo
            return elem.Category?.BuiltInCategory == BuiltInCategory.OST_PipeCurves;
        }

        private void AplicarDesvio(Document doc, Element tubo, LocationCurve curvaTubo, BoundingBoxXYZ boxViga,
                           double tuboCentroZ, double diametroExterno, double espessuraIsolamento,
                           Views.Climatizacao.ClashResolverView janela, TipoConflito tipoConflito)
        {
            Views.Climatizacao.Direcao direcaoEscolhida = janela.DirecaoEscolhida;

            XYZ vetorDirecaoUnitario = null;
            XYZ direcaoTubo = (curvaTubo.Curve.GetEndPoint(1) - curvaTubo.Curve.GetEndPoint(0)).Normalize();
            XYZ vetorDireita = direcaoTubo.CrossProduct(XYZ.BasisZ);

            if (direcaoEscolhida == Views.Climatizacao.Direcao.Cima)
                vetorDirecaoUnitario = XYZ.BasisZ;
            else if (direcaoEscolhida == Views.Climatizacao.Direcao.Baixo)
                vetorDirecaoUnitario = -XYZ.BasisZ;
            else if (direcaoEscolhida == Views.Climatizacao.Direcao.Direita)
                vetorDirecaoUnitario = vetorDireita;
            else
                vetorDirecaoUnitario = -vetorDireita;

            double margemLateralPes = janela.MargemLateralCm / 30.48;
            double folgaPes = janela.FolgaCm / 30.48;

            // Calcular distância
            double distanciaParaSair = 0;
            XYZ pontoRef = curvaTubo.Curve.GetEndPoint(0);

            List<XYZ> cantosObstaculo = new List<XYZ>() {
        boxViga.Min, boxViga.Max,
        new XYZ(boxViga.Min.X, boxViga.Min.Y, boxViga.Max.Z),
        new XYZ(boxViga.Min.X, boxViga.Max.Y, boxViga.Min.Z),
        new XYZ(boxViga.Max.X, boxViga.Min.Y, boxViga.Min.Z),
        new XYZ(boxViga.Max.X, boxViga.Max.Y, boxViga.Max.Z),
        new XYZ(boxViga.Min.X, boxViga.Max.Y, boxViga.Max.Z),
        new XYZ(boxViga.Max.X, boxViga.Min.Y, boxViga.Max.Z)
    };

            foreach (XYZ canto in cantosObstaculo)
            {
                XYZ vetorDoCentroAteCanto = canto - pontoRef;
                double distanciaProjetada = vetorDoCentroAteCanto.DotProduct(vetorDirecaoUnitario);
                if (distanciaProjetada > distanciaParaSair) distanciaParaSair = distanciaProjetada;
            }

            if (distanciaParaSair < 0) distanciaParaSair = 0;

            double distanciaTotal = distanciaParaSair + diametroExterno / 2 + espessuraIsolamento + folgaPes;
            XYZ vetorDesvioFinal = vetorDirecaoUnitario * distanciaTotal;

            List<XYZ> rota = Utils.CalculadoraDesvio.CalcularRota90Graus(
                curvaTubo.Curve.GetEndPoint(0),
                curvaTubo.Curve.GetEndPoint(1),
                boxViga,
                tuboCentroZ,
                vetorDesvioFinal,
                margemLateralPes
            );

            using (Transaction t = new Transaction(doc, "Resolver Conflito"))
            {
                t.Start();
                try
                {
                    // ✅ SEMPRE faz cast para Pipe (tubulação)
                    var pipe = tubo as Autodesk.Revit.DB.Plumbing.Pipe;
                    if (pipe != null)
                    {
                        Utils.ModeladorTubulacao.CriarDesvio90(doc, pipe, rota);
                        TaskDialog.Show("Sucesso", "Desvio aplicado com sucesso! 🎯");
                    }
                    else
                    {
                        throw new Exception("Elemento selecionado não é uma tubulação válida.");
                    }

                    t.Commit();
                }
                catch (Exception ex)
                {
                    t.RollBack();
                    TaskDialog.Show("Erro", "Falha ao aplicar desvio: " + ex.Message);
                }
            }

        }
    }
}