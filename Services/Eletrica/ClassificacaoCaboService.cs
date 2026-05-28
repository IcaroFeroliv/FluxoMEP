using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace AirConditioningClash.Services.Eletrica
{
    public class ClassificacaoCaboService
    {
        public class CaboClassificado
        {
            public string NomeOriginal { get; set; }
            public string Disciplina { get; set; }
            public string TipoCabo { get; set; }
            public string Descricao { get; set; }
        }

        private static readonly Dictionary<string, string> DisciplinaMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "ARC", "INSTALAÇÕES ELÉTRICAS (INCLUSIVE ENTRADA DE ENERGIA E QDCS)" },
            { "BT", "INSTALAÇÕES ELÉTRICAS (INCLUSIVE ENTRADA DE ENERGIA E QDCS)" },
            { "IL", "INSTALAÇÕES ELÉTRICAS (INCLUSIVE ENTRADA DE ENERGIA E QDCS)" },
            { "IL EM", "INSTALAÇÕES ELÉTRICAS (INCLUSIVE ENTRADA DE ENERGIA E QDCS)" },
            { "IL-EM", "INSTALAÇÕES ELÉTRICAS (INCLUSIVE ENTRADA DE ENERGIA E QDCS)" },
            { "IL-EX", "INSTALAÇÕES ELÉTRICAS (INCLUSIVE ENTRADA DE ENERGIA E QDCS)" },
            { "TC", "INSTALAÇÕES ELÉTRICAS (INCLUSIVE ENTRADA DE ENERGIA E QDCS)" },
            { "DAI", "INSTALAÇÕES ELÉTRICAS (INCLUSIVE ENTRADA DE ENERGIA E QDCS)" },
            { "ENE", "INSTALAÇÕES ELÉTRICAS (INCLUSIVE ENTRADA DE ENERGIA E QDCS)" },
            { "SPDA", "INSTALAÇÕES DE SPDA" },
            { "SEG", "SISTEMAS DE CFTV" },
            { "SON", "AUDIOVISUAL-SONORIZAÇÃO" },
            { "CAB", "INSTALAÇÕES DE REDE DE LÓGICA E TELEFONIA" },
            { "CABO", "INSTALAÇÕES DE REDE DE LÓGICA E TELEFONIA" },
        };

        public CaboClassificado Classificar(string nomeParametro)
        {
            var resultado = new CaboClassificado
            {
                NomeOriginal = nomeParametro,
                Disciplina = "NÃO IDENTIFICADO",
                TipoCabo = nomeParametro,
                Descricao = ""
            };

            if (string.IsNullOrWhiteSpace(nomeParametro))
                return resultado;

            // Identificar disciplina
            resultado.Disciplina = IdentificarDisciplina(nomeParametro);

            // Extrair tipo de cabo
            resultado.TipoCabo = ExtrairTipoCabo(nomeParametro);

            // Gerar descrição
            resultado.Descricao = GerarDescricao(resultado.Disciplina, resultado.TipoCabo);

            return resultado;
        }

        private string IdentificarDisciplina(string texto)
        {
            foreach (var prefixo in DisciplinaMap.Keys)
            {
                if (texto.StartsWith(prefixo, StringComparison.OrdinalIgnoreCase))
                    return DisciplinaMap[prefixo];
            }

            return "NÃO IDENTIFICADO";
        }

        private string ExtrairTipoCabo(string texto)
        {
            var match = Regex.Match(texto, @"#[\d.,]+");
            if (match.Success)
            {
                string tipoCabo = match.Value;
                if (texto.IndexOf("HEPR", StringComparison.OrdinalIgnoreCase) >= 0)
                    tipoCabo += " HEPR";
                return tipoCabo;
            }

            return texto;
        }

        private string GerarDescricao(string disciplina, string tipoCabo)
        {
            switch (disciplina)
            {
                case "INSTALAÇÕES ELÉTRICAS (INCLUSIVE ENTRADA DE ENERGIA E QDCS)":
                    return GerarDescricaoEletrica(tipoCabo);

                case "INSTALAÇÕES DE SPDA":
                    return GerarDescricaoSPDA(tipoCabo);

                case "SISTEMAS DE CFTV":
                    return GerarDescricaoCFTV(tipoCabo);

                case "AUDIOVISUAL-SONORIZAÇÃO":
                    return GerarDescricaoSon(tipoCabo);

                case "INSTALAÇÕES DE REDE DE LÓGICA E TELEFONIA":
                    return GerarDescricaoRede(tipoCabo);

                default:
                    return "";
            }
        }

        private string GerarDescricaoEletrica(string tipoCabo)
        {
            var match = Regex.Match(tipoCabo, @"#([\d.,]+)");
            if (match.Success)
            {
                string numero = match.Groups[1].Value;
                if (tipoCabo.Contains("HEPR"))
                    return $"CABO DE {numero}MM HEPR";
                else
                    return $"CABO DE {numero}MM";
            }

            return $"CABO ALARME DE INCÊNDIO - {tipoCabo}";
        }

        private string GerarDescricaoSPDA(string tipoCabo)
        {
            var match = Regex.Match(tipoCabo, @"#([\d.,]+)");
            if (match.Success)
            {
                string numero = match.Groups[1].Value;
                return $"CABO DE {numero}MM";
            }

            return "";
        }

        private string GerarDescricaoCFTV(string tipoCabo)
        {
            if (tipoCabo.IndexOf("UTP4P", StringComparison.OrdinalIgnoreCase) >= 0)
                return "CABO DE REDE CATEGORIA 6";

            var match = Regex.Match(tipoCabo, @"\(([^)]+)\)");
            if (match.Success)
                return $"CABO {match.Groups[1].Value}";

            return "";
        }

        private string GerarDescricaoSon(string tipoCabo)
        {
            if (tipoCabo.IndexOf("HDMI", StringComparison.OrdinalIgnoreCase) >= 0)
                return "CABO HDMI CONECTORIZADO, COMPLETO E INSTALADO";

            return $"CABO de {tipoCabo}";
        }

        private string GerarDescricaoRede(string tipoCabo)
        {
            if (tipoCabo.IndexOf("UTP4P-6A", StringComparison.OrdinalIgnoreCase) >= 0   )
                return "CABO DE REDE CATEGORIA 6.A";

            if (tipoCabo.IndexOf("UTP4P", StringComparison.OrdinalIgnoreCase) >= 0)
                return "CABO DE REDE CATEGORIA 6";

            return "";
        }
    }
}