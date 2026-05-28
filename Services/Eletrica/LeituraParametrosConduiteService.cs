using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using AirConditioningClash.Models.Eletrica;

namespace AirConditioningClash.Services.Eletrica
{
    public class LeituraParametrosConduiteService
    {
        private readonly HashSet<string> _parametrosMonitorados;

        public LeituraParametrosConduiteService(IEnumerable<string> parametrosMonitorados)
        {
            _parametrosMonitorados = new HashSet<string>(parametrosMonitorados, StringComparer.OrdinalIgnoreCase);
        }

        public ResultadoLeituraConduite Ler(Element conduit)
        {
            var resultado = new ResultadoLeituraConduite();
            var encontrados = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (Parameter parameter in conduit.Parameters)
            {
                if (parameter == null || parameter.Definition == null)
                    continue;

                string nome = parameter.Definition.Name;
                if (!_parametrosMonitorados.Contains(nome))
                    continue;

                encontrados.Add(nome);

                double valor = TryGetNumericValue(parameter);
                if (valor > 0)
                {
                    resultado.ParametrosPreenchidos.Add(new ParametroCaboLido
                    {
                        NomeParametro = nome,
                        ValorLido = valor
                    });
                }
            }

            foreach (var nome in _parametrosMonitorados)
            {
                if (!encontrados.Contains(nome))
                    resultado.ParametrosMonitoradosNaoEncontrados.Add(nome);
            }

            return resultado;
        }

        private double TryGetNumericValue(Parameter parameter)
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
                        double parsed;
                        if (double.TryParse(parameter.AsString(), out parsed))
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