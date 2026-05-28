using Autodesk.Revit.DB;

namespace AirConditioningClash.Models.Eletrica
{
    public class ResultadoSelecaoTipoConduite
    {
        public bool Sucesso { get; set; }
        public string NomeTipoEscolhido { get; set; }
        public ElementId TypeId { get; set; }
        public string SiglaUsada { get; set; }
        public bool UsouFallbackCab { get; set; }
        public string Mensagem { get; set; }
    }
}