using System;
using System.Linq;
using AirConditioningClash.Models.Eletrica;

namespace AirConditioningClash.Services.Eletrica
{
    public class DimensionamentoEletricaService
    {
        private readonly CatalogoInfraestruturaService _catalogoInfraestruturaService;

        public DimensionamentoEletricaService(CatalogoInfraestruturaService catalogoInfraestruturaService)
        {
            _catalogoInfraestruturaService = catalogoInfraestruturaService;
        }

        public DimensionamentoResult Dimensionar(DimensionamentoRequest request)
        {
            var result = new DimensionamentoResult();

            if (request == null)
            {
                result.Sucesso = false;
                result.Mensagem = "A requisição de dimensionamento está nula.";
                return result;
            }

            if (request.Cabos == null || !request.Cabos.Any(x => x.Quantidade > 0))
            {
                result.Sucesso = false;
                result.Mensagem = "Informe ao menos um cabo com quantidade maior que zero.";
                return result;
            }

            if (request.FatorOcupacao <= 0 || request.FatorOcupacao > 1)
            {
                result.Sucesso = false;
                result.Mensagem = "O fator de ocupação deve estar entre 0 e 1.";
                return result;
            }

            double areaTotalCabos = 0.0;

            foreach (var cabo in request.Cabos.Where(x => x.Quantidade > 0))
            {
                double areaUnitario = CalcularAreaCircular(cabo.DiametroExternoMm);
                double areaTotalItem = areaUnitario * cabo.Quantidade;

                areaTotalCabos += areaTotalItem;

                result.Detalhes.Add(new DetalheCalculoItem
                {
                    SecaoNominal = cabo.SecaoNominal,
                    DiametroExternoMm = cabo.DiametroExternoMm,
                    Quantidade = cabo.Quantidade,
                    AreaUnitarioMm2 = areaUnitario,
                    AreaTotalMm2 = areaTotalItem
                });
            }

            double areaMinimaInfra = areaTotalCabos / request.FatorOcupacao;

            var infraestrutura = _catalogoInfraestruturaService.ObterMenorCompativel(
                request.TipoInfraestrutura,
                areaMinimaInfra);

            result.AreaTotalCabosMm2 = areaTotalCabos;
            result.AreaMinimaInfraMm2 = areaMinimaInfra;
            result.InfraestruturaSelecionada = infraestrutura;

            if (infraestrutura == null)
            {
                result.Sucesso = false;
                result.Mensagem = "Nenhum item de infraestrutura atende à área mínima calculada.";
                return result;
            }

            result.Sucesso = true;
            result.Mensagem = $"Infraestrutura selecionada: {infraestrutura.NomeComercial}";
            return result;
        }

        private double CalcularAreaCircular(double diametroMm)
        {
            return Math.PI * Math.Pow(diametroMm, 2) / 4.0;
        }
    }
}