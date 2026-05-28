namespace AirConditioningClash.Models.Eletrica
{
    public class InfrastructureCatalogItem
    {
        public EnumInfraestruturaTipo Tipo { get; set; }

        public string NomeComercial { get; set; }

        public double AreaInternaMm2 { get; set; }

        // Para dutos/eletrodutos
        public double? DiametroInternoMm { get; set; }

        // Para eletrocalha
        public double? LarguraMm { get; set; }
        public double? AlturaMm { get; set; }

        // Valor nominal comercial que será usado para aplicar no parâmetro do Revit.
        // Ex.: 0.75, 1.0, 1.25, 1.5, 2.0...
        public double? DiametroNominalPolegadas { get; set; }

        public bool EhEletroduto
        {
            get { return DiametroNominalPolegadas.HasValue; }
        }

        public bool EhEletrocalha
        {
            get { return LarguraMm.HasValue && AlturaMm.HasValue; }
        }

        public override string ToString()
        {
            return $"{Tipo} - {NomeComercial} - Área: {AreaInternaMm2:F2} mm²";
        }
    }
}