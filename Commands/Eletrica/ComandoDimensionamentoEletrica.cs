using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.UI;
using AirConditioningClash.Models.Eletrica;
using AirConditioningClash.Services.Eletrica;

namespace AirConditioningClash.Commands.Eletrica
{
    [Transaction(TransactionMode.Manual)]
    public class ComandoDimensionamentoEletrica : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;

            try
            {
                var escopoPrompt = new EscopoProcessamentoPromptService();
                if (!escopoPrompt.TryPerguntarEscopo(out EnumEscopoProcessamentoEletrica escopo))
                    return Result.Cancelled;

                var coletaService = new ColetaConduitesService();
                List<Conduit> conduites = coletaService.ObterConduites(uiDoc, escopo);

                if (conduites == null || conduites.Count == 0)
                {
                    TaskDialog.Show("Dimensionamento Elétrica", "Nenhum conduite encontrado para processamento.");
                    return Result.Succeeded;
                }

                var processamentoService = new ProcessamentoConduiteService(ObterParametrosMonitorados());
                var resultadoLote = new ProcessamentoConduitesResult();

                using (TransactionGroup tg = new TransactionGroup(doc, "Dimensionar conduites em lote"))
                {
                    tg.Start();

                    foreach (Conduit conduit in conduites)
                    {
                        var failureProcessor = new ConduitBatchFailuresPreprocessor();

                        using (Transaction tx = new Transaction(doc, $"Dimensionar conduite {conduit.Id.Value}"))
                        {
                            try
                            {
                                tx.Start();

                                FailureHandlingOptions fho = tx.GetFailureHandlingOptions();
                                fho.SetFailuresPreprocessor(failureProcessor);
                                fho.SetClearAfterRollback(true);
                                tx.SetFailureHandlingOptions(fho);

                                ProcessamentoConduiteItemResult itemResult = processamentoService.Processar(doc, conduit);

                                // Casos que não devem alterar nada no modelo
                                if (itemResult.Status == EnumStatusProcessamentoConduite.IgnoradoSemParametros ||
                                    itemResult.Status == EnumStatusProcessamentoConduite.IgnoradoSemCabosMapeados ||
                                    itemResult.Status == EnumStatusProcessamentoConduite.ErroLeitura ||
                                    itemResult.Status == EnumStatusProcessamentoConduite.ErroCalculo ||
                                    itemResult.Status == EnumStatusProcessamentoConduite.ErroAplicacao)
                                {
                                    tx.RollBack();
                                    resultadoLote.Itens.Add(itemResult);
                                    continue;
                                }

                                TransactionStatus status = tx.Commit();

                                if (status != TransactionStatus.Committed)
                                {
                                    itemResult.Status = EnumStatusProcessamentoConduite.ErroAplicacao;
                                    itemResult.Mensagem = string.IsNullOrWhiteSpace(failureProcessor.UltimaMensagemErro)
                                        ? "Falha ao aplicar alterações no conduite."
                                        : failureProcessor.UltimaMensagemErro;

                                    resultadoLote.Itens.Add(itemResult);
                                    continue;
                                }

                                resultadoLote.Itens.Add(itemResult);
                            }
                            catch (Exception ex)
                            {
                                if (tx.GetStatus() == TransactionStatus.Started)
                                    tx.RollBack();

                                resultadoLote.Itens.Add(new ProcessamentoConduiteItemResult
                                {
                                    ElementId = conduit.Id,
                                    Status = EnumStatusProcessamentoConduite.ErroAplicacao,
                                    Mensagem = ex.Message
                                });
                            }
                        }
                    }

                    tg.Assimilate();
                }

                var exportacaoLogService = new ExportacaoLogProcessamentoService();
                string caminhoLog = exportacaoLogService.ExportarTxt(escopo, resultadoLote);

                TaskDialog.Show(
                    "Dimensionamento Elétrica",
                    MontarResumoFinal(escopo, resultadoLote, caminhoLog));

                return Result.Succeeded;
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return Result.Cancelled;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                TaskDialog.Show("Erro", $"Erro ao executar o dimensionamento elétrico.\n\n{ex.Message}");
                return Result.Failed;
            }
        }

        private string MontarResumoFinal(
            EnumEscopoProcessamentoEletrica escopo,
            ProcessamentoConduitesResult resultado,
            string caminhoLog)
        {
            var sb = new System.Text.StringBuilder();

            sb.AppendLine("Resumo do processamento");
            sb.AppendLine();
            sb.AppendLine($"Escopo: {escopo}");
            sb.AppendLine($"Total analisado: {resultado.Total}");
            sb.AppendLine($"Sucessos: {resultado.Sucessos}");
            sb.AppendLine($"Ignorados sem parâmetros: {resultado.IgnoradosSemParametros}");
            sb.AppendLine($"Ignorados sem cabos mapeados: {resultado.IgnoradosSemCabosMapeados}");
            sb.AppendLine($"Erros de leitura: {resultado.ErrosLeitura}");
            sb.AppendLine($"Erros de cálculo: {resultado.ErrosCalculo}");
            sb.AppendLine($"Erros de aplicação: {resultado.ErrosAplicacao}");
            sb.AppendLine();
            sb.AppendLine("Os detalhes foram exportados para o arquivo:");
            sb.AppendLine(caminhoLog);

            return sb.ToString();
        }
        private List<string> ObterParametrosMonitorados()
        {
            return new List<string>
            {
                "C - #16 HEPR - TERRA",
                "ARC - #2,5 - FASE A",
                "ARC - #2,5 - FASE B",
                "ARC - #2,5 - FASE C",
                "ARC - #2,5 - NEUTRO",
                "ARC - #2,5 - TERRA",
                "ARC - #2,5 HEPR - FASE A",
                "ARC - #2,5 HEPR - FASE B",
                "ARC - #2,5 HEPR - FASE C",
                "ARC - #2,5 HEPR - TERRA",
                "ARC - #25 HEPR - FASE A",
                "ARC - #25 HEPR - FASE B",
                "ARC - #25 HEPR - FASE C",
                "ARC - #25 HEPR - TERRA",
                "ARC - #4 - FASE A",
                "ARC - #4 - FASE B",
                "ARC - #4 - FASE C",
                "ARC - #4 - NEUTRO",
                "ARC - #4 - TERRA",
                "ARC - #4 HEPR - FASE A",
                "ARC - #4 HEPR - FASE B",
                "ARC - #4 HEPR - FASE C",
                "ARC - #4 HEPR - NEUTRO",
                "ARC - #4 HEPR - TERRA",
                "ARC - #6 HEPR - FASE A",
                "ARC - #6 HEPR - FASE B",
                "ARC - #6 HEPR - FASE C",
                "ARC - #6 HEPR - NEUTRO",
                "ARC - #6 HEPR - TERRA",
                "ARC - #6 - FASE A",
                "ARC - #6 - FASE B",
                "ARC - #6 - FASE C",
                "ARC - #6 - NEUTRO",
                "ARC - #6 - TERRA",
                "ARC - #70 HEPR - TERRA",
                "ARC- #35 HEPR - FASE A",
                "ARC- #35 HEPR - FASE B",
                "ARC- #35 HEPR - FASE C",
                "ARC- #35 HEPR - TERRA",
                "BT - #10 HEPR - FASE A",
                "BT - #10 HEPR - FASE B",
                "BT - #10 HEPR - FASE C",
                "BT - #10 HEPR - NEUTRO",
                "BT - #10 HEPR - TERRA",
                "BT - #120 HEPR - FASE A",
                "BT - #120 HEPR - FASE B",
                "BT - #120 HEPR - FASE C",
                "BT - #120 HEPR - NEUTRO",
                "BT - #120 HEPR - TERRA",
                "BT - #150 HEPR - FASE A",
                "BT - #150 HEPR - FASE B",
                "BT - #150 HEPR - FASE C",
                "BT - #150 HEPR - NEUTRO",
                "BT - #150 HEPR - TERRA",
                "BT - #16 HEPR - FASE A",
                "BT - #16 HEPR - FASE B",
                "BT - #16 HEPR - FASE C",
                "BT - #16 HEPR - NEUTRO",
                "BT - #16 HEPR - TERRA",
                "BT - #185 HEPR - FASE A",
                "BT - #185 HEPR - FASE B",
                "BT - #185 HEPR - FASE C",
                "BT - #185 HEPR - NEUTRO",
                "BT - #185 HEPR - TERRA",
                "BT - #2,5 HEPR - FASE A",
                "BT - #2,5 HEPR - FASE B",
                "BT - #2,5 HEPR - FASE C",
                "BT - #2,5 HEPR - NEUTRO",
                "BT - #2,5 HEPR - TERRA",
                "BT - #240 HEPR - FASE A",
                "BT - #240 HEPR - FASE B",
                "BT - #240 HEPR - FASE C",
                "BT - #240 HEPR - NEUTRO",
                "BT - #240 HEPR - TERRA",
                "BT - #25 HEPR - FASE A",
                "BT - #25 HEPR - FASE B",
                "BT - #25 HEPR - FASE C",
                "BT - #25 HEPR - NEUTRO",
                "BT - #25 HEPR - TERRA",
                "BT - #35 HEPR - FASE A",
                "BT - #35 HEPR - FASE B",
                "BT - #35 HEPR - FASE C",
                "BT - #35 HEPR - NEUTRO",
                "BT - #35 HEPR - TERRA",
                "BT - #4 HEPR - FASE A",
                "BT - #4 HEPR - FASE B",
                "BT - #4 HEPR - FASE C",
                "BT - #4 HEPR - NEUTRO",
                "BT - #4 HEPR - TERRA",
                "BT - #50 HEPR - FASE A",
                "BT - #50 HEPR - FASE B",
                "BT - #50 HEPR - FASE C",
                "BT - #50 HEPR - NEUTRO",
                "BT - #50 HEPR -TERRA",
                "BT - #6 HEPR - FASE A",
                "BT - #6 HEPR - FASE B",
                "BT - #6 HEPR - FASE C",
                "BT - #6 HEPR - NEUTRO",
                "BT - #6 HEPR - TERRA",
                "BT - #70 HEPR - FASE A",
                "BT - #70 HEPR - FASE B",
                "BT - #70 HEPR - FASE C",
                "BT - #70 HEPR - NEUTRO",
                "BT - #70 HEPR - TERRA",
                "BT - #95 HEPR - FASE A",
                "BT - #95 HEPR - FASE B",
                "BT - #95 HEPR - FASE C",
                "BT - #95 HEPR - NEUTRO",
                "BT - #95 HEPR - TERRA",
                "CAB - CPFoMM3F",
                "CAB - CPFoSM6F",
                "CAB - CPU4P",
                "CAB - CSFoMM2F",
                "CAB - HDMI",
                "CAB - UTP4P",
                "CAB - UTP4P_CAT.6A",
                "CAB - UTP4P-6A",
                "CABO - BANCO",
                "CABO - CÓDIGO",
                "DAI - 2x1,5mm²",
                "DAI - Vcc",
                "DAI - UTP4P",
                "ENE - 1x50mm2 - COBRE NU",
                "ENE - 1x70mm2 - COBRE NU",
                "ENE - 3x50mm2 - EPR (12/20kV)",
                "IL - #2,5 FASE A",
                "IL - #2,5 FASE B",
                "IL - #2,5 FASE C",
                "IL - #2,5 NEUTRO",
                "IL - #2,5 RETORNO",
                "IL - #2,5 TERRA",
                "IL - #4 FASE A",
                "IL - #4 FASE B",
                "IL - #4 FASE C",
                "IL - #4 NEUTRO",
                "IL - #4 TERRA",
                "IL - #6 FASE A",
                "IL - #6 FASE B",
                "IL - #6 TERRA",
                "IL - #10 FASE A",
                "IL - #10 FASE B",
                "IL EM - #2,5 FASE A",
                "IL EM - #2,5 NEUTRO",
                "IL EM - #2,5 TERRA",
                "IL-EM - #2,5 - FASE A",
                "IL-EM - #2,5 - NEUTRO",
                "IL-EM - #2,5 - TERRA",
                "IL - #10 TERRA",
                "IL-EX - #2,5 HEPR - FASE A",
                "IL-EX - #2,5 HEPR - FASE B",
                "IL-EX - #2,5 HEPR - FASE C",
                "IL-EX - #2,5 HEPR - NEUTRO",
                "IL-EX - #2,5 HEPR - RETORNO",
                "IL-EX - #2,5 HEPR - TERRA",
                "SEG - (2x1mm)",
                "SEG - (4x0,40mm MC)",
                "SEG - CPFoMM3F",
                "SEG - CPFoSM6F",
                "SEG - CPU4P",
                "SEG - HDMI",
                "SEG - UTP4P",
                "SEG - UTP4P - EXT.",
                "TC - #2,5 - FASE A",
                "TC - #2,5 - FASE B",
                "TC - #2,5 - FASE C",
                "TC - #2,5 - NEUTRO",
                "TC - #2,5 - TERRA",
                "TC - #4 - FASE A",
                "TC - #4 - FASE B",
                "TC - #4 - FASE C",
                "TC - #4 - NEUTRO",
                "TC - #4 - TERRA",
                "TC - #6 - FASE A",
                "TC - #6 - FASE B",
                "TC - #6 - FASE C",
                "TC - #6 - NEUTRO",
                "TC - #6 - TERRA",
                "SPDA - #6 TERRA",
                "SPDA - #16 TERRA",
                "SON - # 1,5mm2",
                "SON - # 2,5mm2",
                "SON - HDMI",
                "SON - PP # 1,5mm2",
                "SON - PP # 2,5mm2",
                "TC - #10 - FASE A",
                "TC - #10 - FASE B",
                "TC - #10 - FASE C",
                "TC - #10 - NEUTRO",
                "TC - #10 - TERRA"
            };
        }
    }
}