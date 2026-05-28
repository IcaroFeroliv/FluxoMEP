using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using AirConditioningClash.Models.Eletrica;

namespace AirConditioningClash.Services.Eletrica
{
    public class ProcessamentoExtracacaoCabosService
    {
        private readonly ColetaElementosInfraestruturaService _coletaService;
        private readonly LeituraDinamicaParametrosService _leituraService;

        public ProcessamentoExtracacaoCabosService(
            IEnumerable<string> parametrosMonitorados = null)
        {
            _coletaService = new ColetaElementosInfraestruturaService();
            _leituraService = new LeituraDinamicaParametrosService(parametrosMonitorados);
        }

        public ExtracacaoResultado Processar(Document doc, ElementId phaseId = null)
        {
            var resultado = new ExtracacaoResultado();

            try
            {
                var conduites = _coletaService.ObterConduites(doc, phaseId);
                var bandejas = _coletaService.ObterBandejasCabos(doc, phaseId);

                // Processar conduites
                foreach (var conduit in conduites)
                {
                    ProcessarElemento(conduit, "Conduit", resultado);
                    resultado.ConduitesProcessados++;
                }

                // Processar bandejas
                foreach (var bandeja in bandejas)
                {
                    ProcessarElemento(bandeja, "CableTray", resultado);
                    resultado.BandejasProcessadas++;
                }
            }
            catch (Exception ex)
            {
                resultado.ErrosProcessamento++;
                // Log do erro se necessário
            }

            return resultado;
        }

        private void ProcessarElemento(
            Element elemento,
            string tipoElemento,
            ExtracacaoResultado resultado)
        {
            try
            {
                double comprimento = _coletaService.ObterComprimentoMetros(elemento);
                if (comprimento <= 0)
                    return;

                var parametrosPreenchidos = _leituraService.LerParametrosPreenchidos(elemento);

                foreach (var kvp in parametrosPreenchidos)
                {
                    string nomeParametro = kvp.Key;
                    double quantidade = kvp.Value;

                    var item = new ExtracacaoCaboItem(
                        elemento.Id,
                        tipoElemento,
                        comprimento,
                        nomeParametro,
                        quantidade);

                    resultado.Itens.Add(item);
                }
            }
            catch
            {
                resultado.ErrosProcessamento++;
            }
        }
    }
}