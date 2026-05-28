using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using AirConditioningClash.Models.Eletrica;
using AirConditioningClash.Services.Eletrica;
using AirConditioningClash.ViewModels;
using AirConditioningClash.Views;

namespace AirConditioningClash.Commands.Eletrica
{
    [Transaction(TransactionMode.ReadOnly)]
    public class ComandoExtracacaoCabosExcel : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                Document doc = commandData.Application.ActiveUIDocument.Document;

                // 1. Abrir janela de filtro
                var viewModel = new FiltroCabosViewModel(doc);
                var janela = new AirConditioningClash.Views.ELE.FiltroCabosView { DataContext = viewModel };
                viewModel.CloseAction = () => janela.Close();
                janela.ShowDialog();

                if (!viewModel.ExportarConfirmado)
                    return Result.Cancelled;

                ElementId phaseId = viewModel.AplicarFiltro
                    ? viewModel.FaseSelecionada?.Id
                    : null;

                // 2. Processar extra��o de cabos
                var parametrosMonitorados = ObterParametrosMonitorados();
                var processamentoService = new ProcessamentoExtracacaoCabosService(parametrosMonitorados);
                ExtracacaoResultado resultado = processamentoService.Processar(doc, phaseId);

                if (resultado.TotalElementos == 0)
                {
                    string msgFiltro = phaseId != null
                        ? $" na fase \"{viewModel.FaseSelecionada?.Name}\""
                        : "";
                    TaskDialog.Show(
                        "Extra��o de Cabos",
                        $"Nenhum conduite ou bandeja encontrada{msgFiltro}.");
                    return Result.Succeeded;
                }

                // 3. Exportar para Excel
                var exportacaoService = new ExportacaoExcelCabosService();
                string caminhoExcel = exportacaoService.ExportarDetalhado(resultado);

                // 4. Exibir resultado
                string resumo = MontarResumoFinal(resultado, caminhoExcel, viewModel);
                TaskDialog.Show("Extra��o de Cabos - Sucesso", resumo);

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                TaskDialog.Show("Erro", $"Erro ao executar extra��o de cabos.\n\n{ex.Message}");
                return Result.Failed;
            }
        }

        private string MontarResumoFinal(ExtracacaoResultado resultado, string caminhoExcel, FiltroCabosViewModel viewModel)
        {
            var sb = new System.Text.StringBuilder();

            sb.AppendLine("EXTRA��O DE CABOS - RELAT�RIO FINAL");
            sb.AppendLine();

            if (viewModel.AplicarFiltro && viewModel.FaseSelecionada != null)
                sb.AppendLine($"Filtro aplicado — fase: {viewModel.FaseSelecionada.Name}");
            else
                sb.AppendLine("Filtro: nenhum (todos os elementos)");

            sb.AppendLine();
            sb.AppendLine($"Conduites processados: {resultado.ConduitesProcessados}");
            sb.AppendLine($"Bandejas de cabos processadas: {resultado.BandejasProcessadas}");
            sb.AppendLine($"Total de elementos: {resultado.TotalElementos}");
            sb.AppendLine($"Total de registros de cabos: {resultado.Itens.Count}");
            sb.AppendLine($"Erros encontrados: {resultado.ErrosProcessamento}");
            sb.AppendLine();
            sb.AppendLine("Arquivo gerado com sucesso:");
            sb.AppendLine(caminhoExcel);

            return sb.ToString();
        }

        private System.Collections.Generic.List<string> ObterParametrosMonitorados()
        {
            // Voc� pode retornar null aqui para ler todos os par�metros
            // Ou manter a lista existente do projeto
            return null;
        }
    }
}