using System.Collections.Generic;

namespace AirConditioningClash.Models.Eletrica
{
    public class DimensionamentoResult
    {
        public bool Sucesso { get; set; }
        public string Mensagem { get; set; }

        public double AreaTotalCabosMm2 { get; set; }
        public double AreaMinimaInfraMm2 { get; set; }

        public InfrastructureCatalogItem InfraestruturaSelecionada { get; set; }

        public List<DetalheCalculoItem> Detalhes { get; set; } = new List<DetalheCalculoItem>();
    }

    public class DetalheCalculoItem
    {
        public string SecaoNominal { get; set; }
        public double DiametroExternoMm { get; set; }
        public int Quantidade { get; set; }
        public double AreaUnitarioMm2 { get; set; }
        public double AreaTotalMm2 { get; set; }
    }
}