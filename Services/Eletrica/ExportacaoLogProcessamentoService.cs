using System;
using System.IO;
using System.Linq;
using System.Text;
using AirConditioningClash.Models.Eletrica;

namespace AirConditioningClash.Services.Eletrica
{
    public class ExportacaoLogProcessamentoService
    {
        public string ExportarTxt(
            EnumEscopoProcessamentoEletrica escopo,
            ProcessamentoConduitesResult resultado)
        {
            string pastaLogs = ObterPastaLogs();
            Directory.CreateDirectory(pastaLogs);

            string fileName = $"Log_Dimensionamento_Conduites_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
            string fullPath = Path.Combine(pastaLogs, fileName);

            string conteudo = MontarConteudo(escopo, resultado);
            File.WriteAllText(fullPath, conteudo, Encoding.UTF8);

            return fullPath;
        }

        private string ObterPastaLogs()
        {
            string meusDocumentos = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            return Path.Combine(meusDocumentos, "AirConditioningClash", "Logs");
        }

        private string MontarConteudo(
            EnumEscopoProcessamentoEletrica escopo,
            ProcessamentoConduitesResult resultado)
        {
            var sb = new StringBuilder();

            sb.AppendLine("LOG DE PROCESSAMENTO - DIMENSIONAMENTO DE CONDUITES");
            sb.AppendLine(new string('=', 70));
            sb.AppendLine($"Data/Hora: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
            sb.AppendLine($"Escopo: {escopo}");
            sb.AppendLine();

            sb.AppendLine("RESUMO");
            sb.AppendLine(new string('-', 70));
            sb.AppendLine($"Total analisado: {resultado.Total}");
            sb.AppendLine($"Sucessos: {resultado.Sucessos}");
            sb.AppendLine($"Ignorados sem parâmetros: {resultado.IgnoradosSemParametros}");
            sb.AppendLine($"Ignorados sem cabos mapeados: {resultado.IgnoradosSemCabosMapeados}");
            sb.AppendLine($"Erros de leitura: {resultado.ErrosLeitura}");
            sb.AppendLine($"Erros de cálculo: {resultado.ErrosCalculo}");
            sb.AppendLine($"Erros de aplicação: {resultado.ErrosAplicacao}");
            sb.AppendLine();

            EscreverSecao(
                sb,
                "SUCESSOS",
                resultado.Itens
                    .Where(x => x.Status == EnumStatusProcessamentoConduite.Sucesso)
                    .Select(x => $"Id {x.ElementId.Value} | Material: {x.MaterialSelecionado} | Tipo Atual: {x.NomeTipoAtual} | Diâmetro: {x.DiametroFinal} | Sigla: {x.SiglaIdentificada}")
                    .ToList());

            EscreverSecao(
                sb,
                "IGNORADOS SEM PARÂMETROS",
                resultado.Itens
                    .Where(x => x.Status == EnumStatusProcessamentoConduite.IgnoradoSemParametros)
                    .Select(x => $"Id {x.ElementId.Value} | {x.Mensagem}")
                    .ToList());

            EscreverSecao(
                sb,
                "IGNORADOS SEM CABOS MAPEADOS",
                resultado.Itens
                    .Where(x => x.Status == EnumStatusProcessamentoConduite.IgnoradoSemCabosMapeados)
                    .Select(x => $"Id {x.ElementId.Value} | {x.Mensagem}")
                    .ToList());

            EscreverSecao(
                sb,
                "ERROS DE LEITURA",
                resultado.Itens
                    .Where(x => x.Status == EnumStatusProcessamentoConduite.ErroLeitura)
                    .Select(x => $"Id {x.ElementId.Value} | {x.Mensagem}")
                    .ToList());

            EscreverSecao(
                sb,
                "ERROS DE CÁLCULO",
                resultado.Itens
                    .Where(x => x.Status == EnumStatusProcessamentoConduite.ErroCalculo)
                    .Select(x => $"Id {x.ElementId.Value} | {x.Mensagem}")
                    .ToList());

            EscreverSecao(
                sb,
                "ERROS DE APLICAÇÃO",
                resultado.Itens
                    .Where(x => x.Status == EnumStatusProcessamentoConduite.ErroAplicacao)
                    .Select(x => $"Id {x.ElementId.Value} | {x.Mensagem}")
                    .ToList());

            return sb.ToString();
        }

        private void EscreverSecao(StringBuilder sb, string titulo, System.Collections.Generic.List<string> linhas)
        {
            sb.AppendLine(titulo);
            sb.AppendLine(new string('-', 70));

            if (linhas == null || linhas.Count == 0)
            {
                sb.AppendLine("Nenhum item.");
                sb.AppendLine();
                return;
            }

            foreach (string linha in linhas)
            {
                sb.AppendLine(linha);
            }

            sb.AppendLine();
        }
    }
}