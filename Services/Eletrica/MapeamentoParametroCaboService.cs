using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using AirConditioningClash.Models.Eletrica;

namespace AirConditioningClash.Services.Eletrica
{
    public class MapeamentoParametroCaboService
    {
        public List<ParametroMapeadoCabo> Mapear(IEnumerable<ParametroCaboLido> parametros)
        {
            var resultado = new List<ParametroMapeadoCabo>();

            foreach (var item in parametros)
            {
                resultado.Add(MapearItem(item));
            }

            return resultado;
        }

        private ParametroMapeadoCabo MapearItem(ParametroCaboLido item)
        {
            string nome = item.NomeParametro ?? string.Empty;
            string sigla = ExtrairSigla(nome);

            var mapped = new ParametroMapeadoCabo
            {
                NomeParametro = nome,
                Quantidade = item.ValorLido,
                Sigla = sigla,
                SuportadoNoCalculo = false
            };

            // UTP / dados
            if (nome.IndexOf("UTP4P_CAT.6A", StringComparison.OrdinalIgnoreCase) >= 0 ||
                nome.IndexOf("UTP4P-6A", StringComparison.OrdinalIgnoreCase) >= 0 ||
                nome.IndexOf("CAT.6A", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                mapped.FamiliaCatalogo = "UTP4P";
                mapped.SecaoNominal = "Cat6A";
                mapped.SuportadoNoCalculo = true;
                return mapped;
            }

            if (nome.IndexOf("UTP4P", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                mapped.FamiliaCatalogo = "UTP4P";
                mapped.SecaoNominal = "Cat6";
                mapped.SuportadoNoCalculo = true;
                return mapped;
            }

            // Cabo PP
            if (Regex.IsMatch(nome, @"PP\s*#\s*1,5", RegexOptions.IgnoreCase))
            {
                mapped.FamiliaCatalogo = "Cabo PP 2 Condutores";
                mapped.SecaoNominal = "1,5";
                mapped.SuportadoNoCalculo = true;
                return mapped;
            }

            if (Regex.IsMatch(nome, @"PP\s*#\s*2,5", RegexOptions.IgnoreCase))
            {
                mapped.FamiliaCatalogo = "Cabo PP 2 Condutores";
                mapped.SecaoNominal = "2,5";
                mapped.SuportadoNoCalculo = true;
                return mapped;
            }

            // 2x1,5 mm² blindado
            if (Regex.IsMatch(nome, @"2x1,5mm", RegexOptions.IgnoreCase))
            {
                mapped.FamiliaCatalogo = "Cabo Blindado 2 Vias";
                mapped.SecaoNominal = "1,5";
                mapped.SuportadoNoCalculo = true;
                return mapped;
            }

            // 4x0,40 mm MC
            if (Regex.IsMatch(nome, @"4x0[,\.]40mm\s*MC", RegexOptions.IgnoreCase))
            {
                mapped.FamiliaCatalogo = "4x0,40mm MC";
                mapped.SecaoNominal = "0,4";
                mapped.SuportadoNoCalculo = true;
                return mapped;
            }

            // solar
            if (nome.IndexOf("Vcc", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                mapped.MotivoNaoMapeado = "Parâmetro identificado como Vcc, mas sem seção nominal explícita.";
                return mapped;
            }

            // cabos com bitola #...
            string secao = ExtrairSecaoNominal(nome);
            if (!string.IsNullOrWhiteSpace(secao))
            {
                bool contemHepr = nome.IndexOf("HEPR", StringComparison.OrdinalIgnoreCase) >= 0;

                mapped.SecaoNominal = secao;
                mapped.FamiliaCatalogo = contemHepr ? "0,6-1kV" : "450-750V";
                mapped.SuportadoNoCalculo = true;
                return mapped;
            }

            mapped.MotivoNaoMapeado = "Parâmetro não possui regra de mapeamento no catálogo atual.";
            return mapped;
        }

        private string ExtrairSigla(string nomeParametro)
        {
            if (string.IsNullOrWhiteSpace(nomeParametro))
                return "CAB";

            int idx = nomeParametro.IndexOf(" - ");
            if (idx > 0)
                return nomeParametro.Substring(0, idx).Trim();

            idx = nomeParametro.IndexOf("-");
            if (idx > 0)
                return nomeParametro.Substring(0, idx).Trim();

            return "CAB";
        }

        private string ExtrairSecaoNominal(string nomeParametro)
        {
            Match match = Regex.Match(nomeParametro, @"#\s*([0-9]+(?:[,\.][0-9]+)?)", RegexOptions.IgnoreCase);
            if (!match.Success)
                return null;

            return match.Groups[1].Value.Replace(".", ",");
        }
    }
}