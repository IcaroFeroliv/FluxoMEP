using Autodesk.Revit.DB;

namespace AirConditioningClash.Models.Eletrica
{
    public class ExtracacaoCaboItem
    {
        public ElementId ElementId { get; set; }
        public string TipoElemento { get; set; } // "Conduit" ou "CableTray"
        public double ComprimentoMetros { get; set; }
        public string NomeParametro { get; set; }
        public double Quantidade { get; set; }
        public double ComprimentoTotal { get; set; } // ComprimentoMetros × Quantidade

        public ExtracacaoCaboItem()
        {
        }

        public ExtracacaoCaboItem(
            ElementId elementId,
            string tipoElemento,
            double comprimentoMetros,
            string nomeParametro,
            double quantidade)
        {
            ElementId = elementId;
            TipoElemento = tipoElemento;
            ComprimentoMetros = comprimentoMetros;
            NomeParametro = nomeParametro;
            Quantidade = quantidade;
            ComprimentoTotal = comprimentoMetros * quantidade;
        }
    }
}