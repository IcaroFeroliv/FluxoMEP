namespace AirConditioningClash.Models.Eletrica
{
    public class ResultadoIdentificacaoMaterialConduite
    {
        public bool Sucesso { get; set; }

        public EnumMaterialConduite Material { get; set; }

        public string NomeTipoAtual { get; set; }

        public string Mensagem { get; set; }
    }
}