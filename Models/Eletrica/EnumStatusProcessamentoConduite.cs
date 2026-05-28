namespace AirConditioningClash.Models.Eletrica
{
    public enum EnumStatusProcessamentoConduite
    {
        Sucesso = 0,
        IgnoradoSemParametros = 1,
        IgnoradoSemCabosMapeados = 2,
        ErroLeitura = 3,
        ErroCalculo = 4,
        ErroAplicacao = 5
    }
}