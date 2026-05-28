using Autodesk.Revit.DB;

namespace AirConditioningClash.Models.Eletrica
{
    public class ProcessamentoConduiteItemResult
    {
        public ElementId ElementId { get; set; }

        public bool Sucesso
        {
            get { return Status == EnumStatusProcessamentoConduite.Sucesso; }
        }

        public EnumStatusProcessamentoConduite Status { get; set; }

        public string Mensagem { get; set; }

        public string SiglaIdentificada { get; set; }

        public string MaterialSelecionado { get; set; }

        public string NomeTipoAtual { get; set; }

        public string TipoEscolhido { get; set; }

        public string DiametroFinal { get; set; }
    }
}