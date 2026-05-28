using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;

namespace AirConditioningClash.Services.Eletrica
{
    public class LeituraDinamicaParametrosService
    {
        private readonly HashSet<string> _parametrosMonitorados;

        public LeituraDinamicaParametrosService(IEnumerable<string> parametrosMonitorados = null)
        {
            if (parametrosMonitorados != null)
            {
                _parametrosMonitorados = new HashSet<string>(
                    parametrosMonitorados,
                    StringComparer.OrdinalIgnoreCase);
            }
            else
            {
                _parametrosMonitorados = null;
            }
        }

        /// <summary>
        /// Lê todos os parâmetros preenchidos do elemento.
        /// Se parametrosMonitorados foi fornecido, filtra apenas esses.
        /// </summary>
        public Dictionary<string, double> LerParametrosPreenchidos(Element elemento)
        {
            var resultado = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

            if (elemento?.Parameters == null)
                return resultado;

            foreach (Parameter param in elemento.Parameters)
            {
                if (param?.Definition == null)
                    continue;

                string nomParametro = param.Definition.Name;

                // Se há lista de monitorados, valida
                if (_parametrosMonitorados != null && !_parametrosMonitorados.Contains(nomParametro))
                    continue;

                double valor = ExtrairValorNumerico(param);
                if (valor > 0)
                {
                    resultado[nomParametro] = valor;
                }
            }

            return resultado;
        }

        /// <summary>
        /// Obtém todos os nomes de parâmetros do elemento, filtrados por monitorados (se definido).
        /// </summary>
        public List<string> ObterNomesParametros(Element elemento)
        {
            var nomes = new List<string>();

            if (elemento?.Parameters == null)
                return nomes;

            foreach (Parameter param in elemento.Parameters)
            {
                if (param?.Definition == null)
                    continue;

                string nomParametro = param.Definition.Name;

                if (_parametrosMonitorados != null && !_parametrosMonitorados.Contains(nomParametro))
                    continue;

                nomes.Add(nomParametro);
            }

            return nomes;
        }

        private double ExtrairValorNumerico(Parameter parameter)
        {
            try
            {
                switch (parameter.StorageType)
                {
                    case StorageType.Integer:
                        return parameter.AsInteger();

                    case StorageType.Double:
                        return parameter.AsDouble();

                    case StorageType.String:
                        string valor = parameter.AsString();
                        if (!string.IsNullOrEmpty(valor) && double.TryParse(valor.Replace(",", "."), out double parsed))
                            return parsed;
                        return 0;

                    default:
                        return 0;
                }
            }
            catch
            {
                return 0;
            }
        }
    }
}