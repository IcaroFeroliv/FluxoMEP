using System.Collections.Generic;
using System.Linq;
using AirConditioningClash.Models.Eletrica;

namespace AirConditioningClash.Services.Eletrica
{
    public class CatalogoInfraestruturaService
    {
        private readonly List<InfrastructureCatalogItem> _itens;

        public CatalogoInfraestruturaService()
        {
            _itens = CriarCatalogoPadrao();
        }

        public List<InfrastructureCatalogItem> ObterTodos()
        {
            return _itens
                .OrderBy(x => x.Tipo)
                .ThenBy(x => x.AreaInternaMm2)
                .ToList();
        }

        public List<InfrastructureCatalogItem> ObterPorTipo(EnumInfraestruturaTipo tipo)
        {
            return _itens
                .Where(x => x.Tipo == tipo)
                .OrderBy(x => x.AreaInternaMm2)
                .ToList();
        }

        public InfrastructureCatalogItem ObterMenorCompativel(EnumInfraestruturaTipo tipo, double areaMinimaMm2)
        {
            return ObterPorTipo(tipo)
                .FirstOrDefault(x => x.AreaInternaMm2 >= areaMinimaMm2);
        }

        private List<InfrastructureCatalogItem> CriarCatalogoPadrao()
        {
            return new List<InfrastructureCatalogItem>
            {
                // =========================================================
                // PVC RÍGIDO
                // =========================================================
                new InfrastructureCatalogItem
                {
                    Tipo = EnumInfraestruturaTipo.PvcRigido,
                    NomeComercial = "3/4\"",
                    AreaInternaMm2 = 356.3167838,
                    DiametroInternoMm = 21.3,
                    DiametroNominalPolegadas = 0.75
                },
                new InfrastructureCatalogItem
                {
                    Tipo = EnumInfraestruturaTipo.PvcRigido,
                    NomeComercial = "1\"",
                    AreaInternaMm2 = 593.93984375,
                    DiametroInternoMm = 27.5,
                    DiametroNominalPolegadas = 1.00
                },
                new InfrastructureCatalogItem
                {
                    Tipo = EnumInfraestruturaTipo.PvcRigido,
                    NomeComercial = "1.1/4\"",
                    AreaInternaMm2 = 1023.50855375,
                    DiametroInternoMm = 36.1,
                    DiametroNominalPolegadas = 1.25
                },
                new InfrastructureCatalogItem
                {
                    Tipo = EnumInfraestruturaTipo.PvcRigido,
                    NomeComercial = "1.1/2\"",
                    AreaInternaMm2 = 1346.101335,
                    DiametroInternoMm = 41.4,
                    DiametroNominalPolegadas = 1.50
                },
                new InfrastructureCatalogItem
                {
                    Tipo = EnumInfraestruturaTipo.PvcRigido,
                    NomeComercial = "2\"",
                    AreaInternaMm2 = 2189.49984,
                    DiametroInternoMm = 52.8,
                    DiametroNominalPolegadas = 2.00
                },
                new InfrastructureCatalogItem
                {
                    Tipo = EnumInfraestruturaTipo.PvcRigido,
                    NomeComercial = "2.1/2\"",
                    AreaInternaMm2 = 3536.08025375,
                    DiametroInternoMm = 67.1,
                    DiametroNominalPolegadas = 2.50
                },
                new InfrastructureCatalogItem
                {
                    Tipo = EnumInfraestruturaTipo.PvcRigido,
                    NomeComercial = "3\"",
                    AreaInternaMm2 = 4976.26166,
                    DiametroInternoMm = 79.6,
                    DiametroNominalPolegadas = 3.00
                },
                new InfrastructureCatalogItem
                {
                    Tipo = EnumInfraestruturaTipo.PvcRigido,
                    NomeComercial = "4\"",
                    AreaInternaMm2 = 8348.22995375,
                    DiametroInternoMm = 103.1,
                    DiametroNominalPolegadas = 4.00
                },

                // =========================================================
                // FERRO GALVANIZADO
                // =========================================================
                new InfrastructureCatalogItem
                {
                    Tipo = EnumInfraestruturaTipo.FerroGalvanizado,
                    NomeComercial = "3/4\"",
                    AreaInternaMm2 = 456.15365375,
                    DiametroInternoMm = 24.1,
                    DiametroNominalPolegadas = 0.75
                },
                new InfrastructureCatalogItem
                {
                    Tipo = EnumInfraestruturaTipo.FerroGalvanizado,
                    NomeComercial = "1\"",
                    AreaInternaMm2 = 725.81216,
                    DiametroInternoMm = 30.4,
                    DiametroNominalPolegadas = 1.00
                },
                new InfrastructureCatalogItem
                {
                    Tipo = EnumInfraestruturaTipo.FerroGalvanizado,
                    NomeComercial = "1.1/4\"",
                    AreaInternaMm2 = 1194.555375,
                    DiametroInternoMm = 39.0,
                    DiametroNominalPolegadas = 1.25
                },
                new InfrastructureCatalogItem
                {
                    Tipo = EnumInfraestruturaTipo.FerroGalvanizado,
                    NomeComercial = "1.1/2\"",
                    AreaInternaMm2 = 1579.7994834375,
                    DiametroInternoMm = 44.85,
                    DiametroNominalPolegadas = 1.50
                },
                new InfrastructureCatalogItem
                {
                    Tipo = EnumInfraestruturaTipo.FerroGalvanizado,
                    NomeComercial = "2\"",
                    AreaInternaMm2 = 2529.3492734375,
                    DiametroInternoMm = 56.75,
                    DiametroNominalPolegadas = 2.00
                },
                new InfrastructureCatalogItem
                {
                    Tipo = EnumInfraestruturaTipo.FerroGalvanizado,
                    NomeComercial = "2.1/2\"",
                    AreaInternaMm2 = 4099.7065859375,
                    DiametroInternoMm = 72.25,
                    DiametroNominalPolegadas = 2.50
                },
                new InfrastructureCatalogItem
                {
                    Tipo = EnumInfraestruturaTipo.FerroGalvanizado,
                    NomeComercial = "3\"",
                    AreaInternaMm2 = 5667.6606509375,
                    DiametroInternoMm = 84.95,
                    DiametroNominalPolegadas = 3.00
                },
                new InfrastructureCatalogItem
                {
                    Tipo = EnumInfraestruturaTipo.FerroGalvanizado,
                    NomeComercial = "4\"",
                    AreaInternaMm2 = 9511.6785884375,
                    DiametroInternoMm = 110.05,
                    DiametroNominalPolegadas = 4.00
                },

                // =========================================================
                // PEAD
                // =========================================================
                new InfrastructureCatalogItem
                {
                    Tipo = EnumInfraestruturaTipo.Pead,
                    NomeComercial = "1.1/4\"",
                    AreaInternaMm2 = 779.28834375,
                    DiametroInternoMm = 31.5,
                    DiametroNominalPolegadas = 1.25
                },
                new InfrastructureCatalogItem
                {
                    Tipo = EnumInfraestruturaTipo.Pead,
                    NomeComercial = "1.1/2\"",
                    AreaInternaMm2 = 1452.158375,
                    DiametroInternoMm = 43.0,
                    DiametroNominalPolegadas = 1.50
                },
                new InfrastructureCatalogItem
                {
                    Tipo = EnumInfraestruturaTipo.Pead,
                    NomeComercial = "2\"",
                    AreaInternaMm2 = 2026.77014,
                    DiametroInternoMm = 50.8,
                    DiametroNominalPolegadas = 2.00
                },
                new InfrastructureCatalogItem
                {
                    Tipo = EnumInfraestruturaTipo.Pead,
                    NomeComercial = "3\"",
                    AreaInternaMm2 = 4417.734375,
                    DiametroInternoMm = 75.0,
                    DiametroNominalPolegadas = 3.00
                },
                new InfrastructureCatalogItem
                {
                    Tipo = EnumInfraestruturaTipo.Pead,
                    NomeComercial = "4\"",
                    AreaInternaMm2 = 8332.043375,
                    DiametroInternoMm = 103.0,
                    DiametroNominalPolegadas = 4.00
                },

                // =========================================================
                // ELETROCALHA
                // =========================================================
                new InfrastructureCatalogItem
                {
                    Tipo = EnumInfraestruturaTipo.Eletrocalha,
                    NomeComercial = "38x38",
                    AreaInternaMm2 = 1444.0,
                    LarguraMm = 38,
                    AlturaMm = 38
                },
                new InfrastructureCatalogItem
                {
                    Tipo = EnumInfraestruturaTipo.Eletrocalha,
                    NomeComercial = "50x50",
                    AreaInternaMm2 = 2500.0,
                    LarguraMm = 50,
                    AlturaMm = 50
                },
                new InfrastructureCatalogItem
                {
                    Tipo = EnumInfraestruturaTipo.Eletrocalha,
                    NomeComercial = "100x50",
                    AreaInternaMm2 = 5000.0,
                    LarguraMm = 100,
                    AlturaMm = 50
                },
                new InfrastructureCatalogItem
                {
                    Tipo = EnumInfraestruturaTipo.Eletrocalha,
                    NomeComercial = "150x50",
                    AreaInternaMm2 = 7500.0,
                    LarguraMm = 150,
                    AlturaMm = 50
                },
                new InfrastructureCatalogItem
                {
                    Tipo = EnumInfraestruturaTipo.Eletrocalha,
                    NomeComercial = "200x50",
                    AreaInternaMm2 = 10000.0,
                    LarguraMm = 200,
                    AlturaMm = 50
                },
                new InfrastructureCatalogItem
                {
                    Tipo = EnumInfraestruturaTipo.Eletrocalha,
                    NomeComercial = "250x50",
                    AreaInternaMm2 = 12500.0,
                    LarguraMm = 250,
                    AlturaMm = 50
                },
                new InfrastructureCatalogItem
                {
                    Tipo = EnumInfraestruturaTipo.Eletrocalha,
                    NomeComercial = "300x50",
                    AreaInternaMm2 = 15000.0,
                    LarguraMm = 300,
                    AlturaMm = 50
                },
                new InfrastructureCatalogItem
                {
                    Tipo = EnumInfraestruturaTipo.Eletrocalha,
                    NomeComercial = "350x50",
                    AreaInternaMm2 = 17500.0,
                    LarguraMm = 350,
                    AlturaMm = 50
                },
                new InfrastructureCatalogItem
                {
                    Tipo = EnumInfraestruturaTipo.Eletrocalha,
                    NomeComercial = "400x50",
                    AreaInternaMm2 = 20000.0,
                    LarguraMm = 400,
                    AlturaMm = 50
                },
                new InfrastructureCatalogItem
                {
                    Tipo = EnumInfraestruturaTipo.Eletrocalha,
                    NomeComercial = "450x50",
                    AreaInternaMm2 = 22500.0,
                    LarguraMm = 450,
                    AlturaMm = 50
                },
                new InfrastructureCatalogItem
                {
                    Tipo = EnumInfraestruturaTipo.Eletrocalha,
                    NomeComercial = "500x50",
                    AreaInternaMm2 = 25000.0,
                    LarguraMm = 500,
                    AlturaMm = 50
                }
            };
        }
    }
}