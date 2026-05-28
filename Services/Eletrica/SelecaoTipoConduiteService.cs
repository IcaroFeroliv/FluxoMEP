using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using AirConditioningClash.Models.Eletrica;
using AirConditioningClash.Utils;

namespace AirConditioningClash.Services.Eletrica
{
    public class SelecaoTipoConduiteService
    {
        public ResultadoSelecaoTipoConduite SelecionarTipo(
            Document doc,
            string siglaPreferencial,
            EnumMaterialConduite material)
        {
            string descricaoMaterial = ObterDescricaoMaterial(material);
            string sigla = string.IsNullOrWhiteSpace(siglaPreferencial) ? "CAB" : siglaPreferencial.Trim();

            List<ConduitType> tipos = new FilteredElementCollector(doc)
                .OfClass(typeof(ConduitType))
                .Cast<ConduitType>()
                .ToList();

            var matchExato = tipos.FirstOrDefault(x =>
                ContemTexto(x.Name, sigla) &&
                ContemTexto(x.Name, descricaoMaterial));

            if (matchExato != null)
            {
                return new ResultadoSelecaoTipoConduite
                {
                    Sucesso = true,
                    NomeTipoEscolhido = matchExato.Name,
                    TypeId = matchExato.Id,
                    SiglaUsada = sigla,
                    UsouFallbackCab = false
                };
            }

            var fallbackCab = tipos.FirstOrDefault(x =>
                ContemTexto(x.Name, "CAB") &&
                ContemTexto(x.Name, descricaoMaterial));

            if (fallbackCab != null)
            {
                return new ResultadoSelecaoTipoConduite
                {
                    Sucesso = true,
                    NomeTipoEscolhido = fallbackCab.Name,
                    TypeId = fallbackCab.Id,
                    SiglaUsada = "CAB",
                    UsouFallbackCab = true,
                    Mensagem = $"Sigla '{sigla}' não encontrada. Usado fallback 'CAB'."
                };
            }

            return new ResultadoSelecaoTipoConduite
            {
                Sucesso = false,
                SiglaUsada = sigla,
                Mensagem = $"Nenhum tipo de conduite encontrado para material '{descricaoMaterial}' e fallback 'CAB'."
            };
        }

        private bool ContemTexto(string origem, string termo)
        {
            string n1 = StringNormalizeHelper.Normalize(origem);
            string n2 = StringNormalizeHelper.Normalize(termo);
            return n1.Contains(n2);
        }

        private string ObterDescricaoMaterial(EnumMaterialConduite material)
        {
            switch (material)
            {
                case EnumMaterialConduite.FerroGalvanizado:
                    return "ELETRODUTO DE FERRO GALVANIZADO";
                case EnumMaterialConduite.Pead:
                    return "ELETRODUTO DE PEAD";
                case EnumMaterialConduite.PvcRigido:
                    return "ELETRODUTO DE PVC RIGIDO";
                default:
                    return string.Empty;
            }
        }
    }
}