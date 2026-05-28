using System.Collections.Generic;
using System.Linq;

namespace AirConditioningClash.Models.Eletrica
{
    public class ExtracacaoResultado
    {
        public List<ExtracacaoCaboItem> Itens { get; set; } = new List<ExtracacaoCaboItem>();
        public int ConduitesProcessados { get; set; }
        public int BandejasProcessadas { get; set; }
        public int ErrosProcessamento { get; set; }
        public string CaminhoExcelGerado { get; set; }

        public int TotalElementos => ConduitesProcessados + BandejasProcessadas;

        public Dictionary<string, double> ObterResumoParTipo()
        {
            return Itens
                .GroupBy(x => x.NomeParametro)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(x => x.ComprimentoTotal));
        }

        public Dictionary<string, double> ObterResumoParTipoElemento(string tipoElemento)
        {
            return Itens
                .Where(x => x.TipoElemento == tipoElemento)
                .GroupBy(x => x.NomeParametro)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(x => x.ComprimentoTotal));
        }
    }
}