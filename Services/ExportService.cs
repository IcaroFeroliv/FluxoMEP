using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AirConditioningClash.Models;
using Autodesk.Revit.DB;
using System.Text.RegularExpressions;

namespace AirConditioningClash.Services
{
    public class ExportService
    {
        public void ExportarParaPDF(Document doc, List<SheetModel> folhasSelecionadas, string caminhoSaida, string regraNomenclatura)
        {
            // ... verificações de erro iniciais ...

            using (Transaction trans = new Transaction(doc, "ARQ Flow: Exportar PDFs"))
            {
                trans.Start();

                foreach (var folhaModel in folhasSelecionadas)
                {
                    ViewSheet folhaRevit = folhaModel.RevitSheet;

                    // 1. Gera o nome SEM a extensão .pdf (o Revit adiciona automaticamente)
                    string nomeArquivo = MontarNomeArquivo(folhaRevit, regraNomenclatura);

                    // Limpa caracteres inválidos
                    foreach (char c in Path.GetInvalidFileNameChars())
                    {
                        nomeArquivo = nomeArquivo.Replace(c, '_');
                    }

                    // 2. Configurações para forçar o nome exato
                    PDFExportOptions opcoesPDF = new PDFExportOptions
                    {
                        // TRUQUE: Combine = true força o Revit a usar EXATAMENTE o FileName
                        Combine = true,
                        FileName = nomeArquivo,
                        ZoomType = ZoomType.Zoom,
                        ZoomPercentage = 100,
                        PaperFormat = ExportPaperFormat.Default,
                        HideCropBoundaries = true,
                        HideScopeBoxes = true,
                        HideUnreferencedViewTags = true
                    };

                    List<ElementId> idDaFolha = new List<ElementId> { folhaRevit.Id };

                    // 3. Exporta
                    doc.Export(caminhoSaida, idDaFolha, opcoesPDF);
                }

                trans.Commit();
            }
        }

        // Método auxiliar para traduzir a "Regra" (ex: <Número da Folha> - <Nome da Folha>) para valores reais
        private string MontarNomeArquivo(ViewSheet folha, string regra)
        {
            string nomeFinal = regra;

            // Busca todas as tags <Parametro> na regra definida pelo usuário
            var tags = Regex.Matches(regra, @"<(.*?)>");

            foreach (Match match in tags)
            {
                string tagCompleta = match.Value; // Ex: "<ARQ_Disciplina>"
                string nomeParametro = match.Groups[1].Value; // Ex: "ARQ_Disciplina"

                string valor = "";

                // Trata os campos padrão do Revit e busca os demais dinamicamente
                if (nomeParametro == "Número da Folha")
                    valor = folha.SheetNumber;
                else if (nomeParametro == "Nome da Folha")
                    valor = folha.Name;
                else
                    valor = ObterValorParametro(folha, nomeParametro);

                nomeFinal = nomeFinal.Replace(tagCompleta, valor);
            }

            return nomeFinal.Trim();
        }

        private string ObterValorParametro(ViewSheet folha, string nomeParametro)
        {
            Parameter param = folha.LookupParameter(nomeParametro);
            if (param != null && param.HasValue)
            {
                return param.AsString();
            }
            return ""; // Retorna vazio se o parâmetro não existir ou não estiver preenchido
        }
    }
}