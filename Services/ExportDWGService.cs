using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using AirConditioningClash.Models;
using Autodesk.Revit.DB;

namespace AirConditioningClash.Services
{
    public class ExportDWGService
    {
        public void ExportarParaDWG(Document doc, List<SheetModel> folhas, string pasta, string regra, ExportDWGSettings setup)
        {
            DWGExportOptions options = setup.GetDWGExportOptions();

            // Importante para DWG: Geralmente não queremos exportar as views vinculadas como Xrefs separados
            options.MergedViews = true;

            using (Transaction t = new Transaction(doc, "Exportar DWG"))
            {
                t.Start();
                foreach (var model in folhas)
                {
                    string nome = MontarNome(model.RevitSheet, regra);
                    var ids = new List<ElementId> { model.RevitSheet.Id };

                    // O Revit exporta DWG para uma pasta, o nome do arquivo é o segundo parâmetro
                    doc.Export(pasta, nome, ids, options);
                }
                t.Commit();
            }
        }

        private string MontarNome(ViewSheet sheet, string regra)
        {
            string nome = regra;
            var matches = Regex.Matches(regra, @"<(.*?)>");
            foreach (Match m in matches)
            {
                string pNome = m.Groups[1].Value;
                string valor = "";

                if (pNome == "Número da Folha") valor = sheet.SheetNumber;
                else if (pNome == "Nome da Folha") valor = sheet.Name;
                else
                {
                    Parameter param = sheet.LookupParameter(pNome);
                    // Aqui está o pulo do gato: tenta ValueString (números) e depois String (textos)
                    if (param != null && param.HasValue)
                        valor = param.AsValueString() ?? param.AsString() ?? "";
                }

                nome = nome.Replace(m.Value, valor);
            }

            foreach (char c in Path.GetInvalidFileNameChars()) nome = nome.Replace(c, '_');
            return nome;
        }
    }
}