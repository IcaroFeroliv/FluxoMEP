using System.Collections.Generic;

namespace AirConditioningClash.Models.Eletrica
{
    public class ResultadoLeituraConduite
    {
        public List<ParametroCaboLido> ParametrosPreenchidos { get; set; } = new List<ParametroCaboLido>();
        public List<string> ParametrosMonitoradosNaoEncontrados { get; set; } = new List<string>();
    }
}