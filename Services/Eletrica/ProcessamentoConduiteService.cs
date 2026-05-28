using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using AirConditioningClash.Models.Eletrica;

namespace AirConditioningClash.Services.Eletrica
{
    public class ProcessamentoConduiteService
    {
        private readonly List<string> _parametrosMonitorados;
        private readonly LeituraParametrosConduiteService _leituraService;
        private readonly MapeamentoParametroCaboService _mapeamentoService;
        private readonly CatalogoCabosService _catalogoCabosService;
        private readonly CatalogoInfraestruturaService _catalogoInfraService;
        private readonly DimensionamentoEletricaService _dimensionamentoService;
        private readonly AplicacaoConduiteService _aplicacaoService;
        private readonly IdentificacaoMaterialConduiteService _identificacaoMaterialService;

        public ProcessamentoConduiteService(List<string> parametrosMonitorados)
        {
            _parametrosMonitorados = parametrosMonitorados;
            _leituraService = new LeituraParametrosConduiteService(_parametrosMonitorados);
            _mapeamentoService = new MapeamentoParametroCaboService();
            _catalogoCabosService = new CatalogoCabosService();
            _catalogoInfraService = new CatalogoInfraestruturaService();
            _dimensionamentoService = new DimensionamentoEletricaService(_catalogoInfraService);
            _aplicacaoService = new AplicacaoConduiteService();
            _identificacaoMaterialService = new IdentificacaoMaterialConduiteService();
        }

        public ProcessamentoConduiteItemResult Processar(Document doc, Conduit conduit)
        {
            var itemResult = new ProcessamentoConduiteItemResult
            {
                ElementId = conduit.Id
            };

            try
            {
                var identificacaoMaterial = _identificacaoMaterialService.Identificar(conduit);
                if (!identificacaoMaterial.Sucesso)
                {
                    itemResult.Status = EnumStatusProcessamentoConduite.ErroAplicacao;
                    itemResult.Mensagem = identificacaoMaterial.Mensagem;
                    return itemResult;
                }

                itemResult.NomeTipoAtual = identificacaoMaterial.NomeTipoAtual;
                itemResult.MaterialSelecionado = identificacaoMaterial.Material.ToString();

                ResultadoLeituraConduite leitura = _leituraService.Ler(conduit);

                if (leitura.ParametrosPreenchidos.Count == 0)
                {
                    itemResult.Status = EnumStatusProcessamentoConduite.IgnoradoSemParametros;
                    itemResult.Mensagem = "Nenhum parâmetro monitorado com valor maior que zero.";
                    return itemResult;
                }

                List<ParametroMapeadoCabo> mapeados = _mapeamentoService.Mapear(leitura.ParametrosPreenchidos);

                string siglaIdentificada = mapeados
                    .Where(x => !string.IsNullOrWhiteSpace(x.Sigla))
                    .GroupBy(x => x.Sigla)
                    .OrderByDescending(g => g.Count())
                    .Select(g => g.Key)
                    .FirstOrDefault() ?? "CAB";

                itemResult.SiglaIdentificada = siglaIdentificada;

                EnumInfraestruturaTipo tipoInfra = ConverterMaterial(identificacaoMaterial.Material);

                List<CableInputItem> cabosParaCalculo = new List<CableInputItem>();

                foreach (var grupo in mapeados
                    .Where(x => x.SuportadoNoCalculo)
                    .GroupBy(x => new { x.FamiliaCatalogo, x.SecaoNominal }))
                {
                    CableCatalogItem caboCatalogo = _catalogoCabosService
                        .ObterPorFamilia(grupo.Key.FamiliaCatalogo)
                        .FirstOrDefault(x => string.Equals(x.SecaoNominal, grupo.Key.SecaoNominal, StringComparison.OrdinalIgnoreCase));

                    if (caboCatalogo == null)
                        continue;

                    int quantidadeTotal = (int)Math.Round(grupo.Sum(x => x.Quantidade));

                    cabosParaCalculo.Add(new CableInputItem
                    {
                        SecaoNominal = grupo.Key.SecaoNominal,
                        DiametroExternoMm = caboCatalogo.DiametroExternoMm,
                        Quantidade = quantidadeTotal
                    });
                }

                if (!cabosParaCalculo.Any())
                {
                    itemResult.Status = EnumStatusProcessamentoConduite.IgnoradoSemCabosMapeados;
                    itemResult.Mensagem = "Parâmetros encontrados, mas nenhum cabo suportado pôde ser mapeado para cálculo.";
                    return itemResult;
                }

                DimensionamentoRequest request = new DimensionamentoRequest
                {
                    FamiliaCabos = "Misto",
                    TipoInfraestrutura = tipoInfra,
                    FatorOcupacao = 0.40,
                    Cabos = cabosParaCalculo
                };

                DimensionamentoResult calc = _dimensionamentoService.Dimensionar(request);

                if (!calc.Sucesso || calc.InfraestruturaSelecionada == null)
                {
                    itemResult.Status = EnumStatusProcessamentoConduite.ErroCalculo;
                    itemResult.Mensagem = calc.Mensagem;
                    return itemResult;
                }

                _aplicacaoService.AplicarSomenteDiametro(conduit, calc.InfraestruturaSelecionada);

                itemResult.Status = EnumStatusProcessamentoConduite.Sucesso;
                itemResult.Mensagem = "Processado com sucesso.";
                itemResult.TipoEscolhido = identificacaoMaterial.NomeTipoAtual;
                itemResult.DiametroFinal = calc.InfraestruturaSelecionada.NomeComercial;

                return itemResult;
            }
            catch (Exception ex)
            {
                itemResult.Status = EnumStatusProcessamentoConduite.ErroAplicacao;
                itemResult.Mensagem = ex.Message;
                return itemResult;
            }
        }

        private EnumInfraestruturaTipo ConverterMaterial(EnumMaterialConduite material)
        {
            switch (material)
            {
                case EnumMaterialConduite.FerroGalvanizado:
                    return EnumInfraestruturaTipo.FerroGalvanizado;
                case EnumMaterialConduite.Pead:
                    return EnumInfraestruturaTipo.Pead;
                case EnumMaterialConduite.PvcRigido:
                    return EnumInfraestruturaTipo.PvcRigido;
                default:
                    return EnumInfraestruturaTipo.FerroGalvanizado;
            }
        }
    }
}