namespace AirConditioningClash.Models.Eletrica
{
    public class ParametroMapeadoCabo
    {
        public string NomeParametro { get; set; }
        public double Quantidade { get; set; }

        public string Sigla { get; set; }
        public string FamiliaCatalogo { get; set; }
        public string SecaoNominal { get; set; }

        public bool SuportadoNoCalculo { get; set; }
        public string MotivoNaoMapeado { get; set; }
    }
}