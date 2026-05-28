using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.Win32;
using AirConditioningClash.Models.Eletrica;
using ClosedXML.Excel;

namespace AirConditioningClash.Services.Eletrica
{
    public class ExportacaoExcelCabosService
    {
        private readonly ClassificacaoCaboService _classificacaoService;

        public ExportacaoExcelCabosService()
        {
            _classificacaoService = new ClassificacaoCaboService();
        }

        public string ExportarDetalhado(ExtracacaoResultado resultado)
        {
            // Criar diálogo de salvamento
            var saveDialog = new SaveFileDialog
            {
                FileName = $"Levantamento_Detalhado_Cabos_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                DefaultExt = ".xlsx",
                Filter = "Arquivos Excel|*.xlsx|Todos os arquivos|*.*",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            };

            // Mostrar diálogo
            if (saveDialog.ShowDialog() != true)
                return null; // Usuário cancelou

            string caminhoCompleto = saveDialog.FileName;

            using (var workbook = new XLWorkbook())
            {
                // Aba 1: Detalhado
                var wsDetalhado = workbook.Worksheets.Add("Detalhado");
                CriarAbaDetalhada(wsDetalhado, resultado);

                // Aba 2: Resumo por tipo
                var wsResumoPorTipo = workbook.Worksheets.Add("Resumo por Tipo");
                CriarAbaResumoPorTipo(wsResumoPorTipo, resultado);

                // Aba 3: Resumo por tipo de elemento
                var wsResumoPorElemento = workbook.Worksheets.Add("Resumo por Elemento");
                CriarAbaResumoPorElemento(wsResumoPorElemento, resultado);

                // Aba 4: Relatório Formatado (similar ao do Excel VBA)
                var wsRelatorio = workbook.Worksheets.Add("Relatório Formatado");
                CriarAbaRelatorioFormatado(wsRelatorio, resultado);

                workbook.SaveAs(caminhoCompleto);
            }

            resultado.CaminhoExcelGerado = caminhoCompleto;
            return caminhoCompleto;
        }

        private void CriarAbaDetalhada(IXLWorksheet ws, ExtracacaoResultado resultado)
        {
            // Cabeçalho
            ws.Cell(1, 1).Value = "Tipo de Elemento";
            ws.Cell(1, 2).Value = "ID do Elemento";
            ws.Cell(1, 3).Value = "Comprimento (m)";
            ws.Cell(1, 4).Value = "Tipo de Cabo";
            ws.Cell(1, 5).Value = "Quantidade";
            ws.Cell(1, 6).Value = "Comprimento Total (m)";
            ws.Cell(1, 7).Value = "Disciplina";
            ws.Cell(1, 8).Value = "Descrição do Cabo";

            // Formatar cabeçalho
            var headerRange = ws.Range(1, 1, 1, 8);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
            headerRange.Style.Font.FontColor = XLColor.Black;

            // Dados
            int linha = 2;
            foreach (var item in resultado.Itens)
            {
                var classificacao = _classificacaoService.Classificar(item.NomeParametro);

                ws.Cell(linha, 1).Value = item.TipoElemento;
                ws.Cell(linha, 2).Value = item.ElementId.Value;
                ws.Cell(linha, 3).Value = item.ComprimentoMetros;
                ws.Cell(linha, 4).Value = item.NomeParametro;
                ws.Cell(linha, 5).Value = item.Quantidade;
                ws.Cell(linha, 6).Value = item.ComprimentoTotal;
                ws.Cell(linha, 7).Value = classificacao.Disciplina;
                ws.Cell(linha, 8).Value = classificacao.Descricao;

                // Formatar números
                ws.Cell(linha, 3).Style.NumberFormat.Format = "#,##0.00";
                ws.Cell(linha, 5).Style.NumberFormat.Format = "#,##0.00";
                ws.Cell(linha, 6).Style.NumberFormat.Format = "#,##0.00";

                linha++;
            }

            // Auto-dimensionar colunas
            ws.Columns(1, 8).AdjustToContents();
        }

        private void CriarAbaResumoPorTipo(IXLWorksheet ws, ExtracacaoResultado resultado)
        {
            var resumo = resultado.ObterResumoParTipo();

            // Cabeçalho
            ws.Cell(1, 1).Value = "Tipo de Cabo";
            ws.Cell(1, 2).Value = "Conduites (m)";
            ws.Cell(1, 3).Value = "Bandejas de Cabos (m)";
            ws.Cell(1, 4).Value = "Total (m)";
            ws.Cell(1, 5).Value = "Disciplina";
            ws.Cell(1, 6).Value = "Descrição do Cabo";

            var headerRange = ws.Range(1, 1, 1, 6);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
            headerRange.Style.Font.FontColor = XLColor.Black;

            // Dados
            int linha = 2;
            foreach (var tipoCabo in resumo.Keys.OrderBy(k => k))
            {
                var resumoConduites = resultado.ObterResumoParTipoElemento("Conduit");
                var resumoBandejas = resultado.ObterResumoParTipoElemento("CableTray");

                double totalConduites = resumoConduites.ContainsKey(tipoCabo) ? resumoConduites[tipoCabo] : 0;
                double totalBandejas = resumoBandejas.ContainsKey(tipoCabo) ? resumoBandejas[tipoCabo] : 0;
                double total = resumo[tipoCabo];

                var classificacao = _classificacaoService.Classificar(tipoCabo);

                ws.Cell(linha, 1).Value = tipoCabo;
                ws.Cell(linha, 2).Value = totalConduites;
                ws.Cell(linha, 3).Value = totalBandejas;
                ws.Cell(linha, 4).Value = total;
                ws.Cell(linha, 5).Value = classificacao.Disciplina;
                ws.Cell(linha, 6).Value = classificacao.Descricao;

                // Formatar números
                for (int col = 2; col <= 4; col++)
                    ws.Cell(linha, col).Style.NumberFormat.Format = "#,##0.00";

                linha++;
            }

            // Auto-dimensionar
            ws.Columns(1, 6).AdjustToContents();
        }

        private void CriarAbaResumoPorElemento(IXLWorksheet ws, ExtracacaoResultado resultado)
        {
            var resumoConduites = resultado.ObterResumoParTipoElemento("Conduit");
            var resumoBandejas = resultado.ObterResumoParTipoElemento("CableTray");

            // Cabeçalho
            ws.Cell(1, 1).Value = "Tipo de Cabo";
            ws.Cell(1, 2).Value = "Conduites (m)";
            ws.Cell(1, 3).Value = "Bandejas de Cabos (m)";
            ws.Cell(1, 4).Value = "Total (m)";

            var headerRange = ws.Range(1, 1, 1, 4);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
            headerRange.Style.Font.FontColor = XLColor.Black;

            // Dados
            int linha = 2;
            var todosTipos = new HashSet<string>(resumoConduites.Keys);
            todosTipos.UnionWith(resumoBandejas.Keys);

            foreach (var tipoCabo in todosTipos.OrderBy(t => t))
            {
                double totalConduites = resumoConduites.ContainsKey(tipoCabo) ? resumoConduites[tipoCabo] : 0;
                double totalBandejas = resumoBandejas.ContainsKey(tipoCabo) ? resumoBandejas[tipoCabo] : 0;
                double total = totalConduites + totalBandejas;

                ws.Cell(linha, 1).Value = tipoCabo;
                ws.Cell(linha, 2).Value = totalConduites;
                ws.Cell(linha, 3).Value = totalBandejas;
                ws.Cell(linha, 4).Value = total;

                for (int col = 2; col <= 4; col++)
                    ws.Cell(linha, col).Style.NumberFormat.Format = "#,##0.00";

                linha++;
            }

            ws.Columns(1, 4).AdjustToContents();
        }

        private void CriarAbaRelatorioFormatado(IXLWorksheet ws, ExtracacaoResultado resultado)
        {
            // Processar apenas itens válidos (com descrição)
            var itensValidos = resultado.Itens
                .Where(item =>
                {
                    var classif = _classificacaoService.Classificar(item.NomeParametro);
                    return !string.IsNullOrWhiteSpace(classif.Descricao);
                })
                .ToList();

            // Agrupar por disciplina e descrição
            var grupos = itensValidos
                .Select(item =>
                {
                    var classif = _classificacaoService.Classificar(item.NomeParametro);
                    return new
                    {
                        item,
                        classif
                    };
                })
                .GroupBy(x => new { x.classif.Disciplina, x.classif.Descricao })
                .OrderBy(g => g.Key.Disciplina)
                .ThenBy(g => g.Key.Descricao);

            int linha = 1;
            int grupoIndex = 1;
            string disciplinaAnterior = "";

            foreach (var grupo in grupos)
            {
                // Cabeçalho de disciplina
                if (grupo.Key.Disciplina != disciplinaAnterior)
                {
                    ws.Cell(linha, 1).Value = grupoIndex;
                    ws.Cell(linha, 2).Value = grupo.Key.Disciplina;

                    var grupoHeaderRange = ws.Range(linha, 1, linha, 4);
                    grupoHeaderRange.Style.Font.Bold = true;
                    grupoHeaderRange.Style.Fill.BackgroundColor = XLColor.LightBlue;
                    grupoHeaderRange.Style.Font.FontColor = XLColor.Black;

                    linha++;

                    ws.Cell(linha, 1).Value = $"{grupoIndex}.1";
                    ws.Cell(linha, 2).Value = "CABEAMENTO";

                    var cabeamentoRange = ws.Range(linha, 1, linha, 4);
                    cabeamentoRange.Style.Font.Bold = true;
                    cabeamentoRange.Style.Fill.BackgroundColor = XLColor.LightCyan;
                    cabeamentoRange.Style.Font.FontColor = XLColor.Black;

                    linha++;
                    disciplinaAnterior = grupo.Key.Disciplina;
                    grupoIndex++;
                }

                // Agrupar itens iguais dentro do grupo e somar os comprimentos
                var itensAgrupados = grupo
                    .GroupBy(x => x.classif.Descricao)
                    .Select(g => new
                    {
                        descricao = g.Key,
                        totalComprimento = g.Sum(x => x.item.ComprimentoTotal)
                    })
                    .OrderBy(x => x.descricao);

                // Itens do grupo
                int itemIndex = 1;
                foreach (var itemAgrupado in itensAgrupados)
                {
                    ws.Cell(linha, 1).Value = $"{grupoIndex - 1}.1.{itemIndex}";
                    ws.Cell(linha, 2).Value = itemAgrupado.descricao;
                    ws.Cell(linha, 3).Value = "m";
                    ws.Cell(linha, 4).Value = itemAgrupado.totalComprimento;

                    ws.Cell(linha, 4).Style.NumberFormat.Format = "#,##0.00";

                    linha++;
                    itemIndex++;
                }
            }

            // Auto-dimensionar
            ws.Column(1).AdjustToContents();
            ws.Column(2).Width = 50;
            ws.Column(3).AdjustToContents();
            ws.Column(4).AdjustToContents();
        }
    }
}