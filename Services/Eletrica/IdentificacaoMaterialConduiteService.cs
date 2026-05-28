using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using AirConditioningClash.Models.Eletrica;
using AirConditioningClash.Utils;

namespace AirConditioningClash.Services.Eletrica
{
    public class IdentificacaoMaterialConduiteService
    {
        public ResultadoIdentificacaoMaterialConduite Identificar(Conduit conduit)
        {
            var result = new ResultadoIdentificacaoMaterialConduite();

            if (conduit == null)
            {
                result.Sucesso = false;
                result.Mensagem = "Conduite nulo.";
                return result;
            }

            ElementType elementType = conduit.Document.GetElement(conduit.GetTypeId()) as ElementType;
            if (elementType == null)
            {
                result.Sucesso = false;
                result.Mensagem = "Não foi possível obter o tipo do conduite.";
                return result;
            }

            string nomeTipo = elementType.Name ?? string.Empty;
            string nomeNormalizado = StringNormalizeHelper.Normalize(nomeTipo);

            result.NomeTipoAtual = nomeTipo;

            if (Contem(nomeNormalizado, "PEAD"))
            {
                result.Sucesso = true;
                result.Material = EnumMaterialConduite.Pead;
                return result;
            }

            if (Contem(nomeNormalizado, "PVC") && Contem(nomeNormalizado, "RIGIDO"))
            {
                result.Sucesso = true;
                result.Material = EnumMaterialConduite.PvcRigido;
                return result;
            }

            if (Contem(nomeNormalizado, "FERRO GALVANIZADO"))
            {
                result.Sucesso = true;
                result.Material = EnumMaterialConduite.FerroGalvanizado;
                return result;
            }

            // fallback opcional para templates que usam "AÇO GALVANIZADO"
            if (Contem(nomeNormalizado, "ACO GALVANIZADO") || Contem(nomeNormalizado, "AÇO GALVANIZADO"))
            {
                result.Sucesso = true;
                result.Material = EnumMaterialConduite.FerroGalvanizado;
                return result;
            }

            result.Sucesso = false;
            result.Mensagem = $"Não foi possível identificar o material do conduite pelo tipo atual: '{nomeTipo}'.";
            return result;
        }

        private bool Contem(string origemNormalizada, string termo)
        {
            string termoNormalizado = StringNormalizeHelper.Normalize(termo);
            return origemNormalizada.Contains(termoNormalizado);
        }
    }
}