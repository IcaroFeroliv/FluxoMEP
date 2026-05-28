using System.Collections.Generic;

namespace AirConditioningClash.Models.Eletrica
{
    public class DimensionamentoRequest
    {
        public string FamiliaCabos { get; set; }
        public EnumInfraestruturaTipo TipoInfraestrutura { get; set; }
        public double FatorOcupacao { get; set; } = 0.40;
        public List<CableInputItem> Cabos { get; set; } = new List<CableInputItem>();
    }
}