using System.Collections.Generic;
using System.Linq;
using AirConditioningClash.Models.Eletrica;

namespace AirConditioningClash.Services.Eletrica
{
    public class CatalogoCabosService
    {
        private readonly List<CableCatalogItem> _itens;

        public CatalogoCabosService()
        {
            _itens = CriarCatalogoInicial();
        }

        public List<CableCatalogItem> ObterPorFamilia(string familia)
        {
            return _itens
                .Where(x => x.Familia == familia)
                .ToList();
        }

        private List<CableCatalogItem> CriarCatalogoInicial()
        {
            return new List<CableCatalogItem>
            {
                // =====================================================
                // 450-750V
                // =====================================================
                new CableCatalogItem { Familia = "450-750V", SecaoNominal = "1,5", DiametroExternoMm = 2.9, Descricao = "Noflam Antichama BWF Flexível 450/750V - FICAP" },
                new CableCatalogItem { Familia = "450-750V", SecaoNominal = "2,5", DiametroExternoMm = 3.6, Descricao = "Noflam Antichama BWF Flexível 450/750V - FICAP" },
                new CableCatalogItem { Familia = "450-750V", SecaoNominal = "4", DiametroExternoMm = 4.1, Descricao = "Noflam Antichama BWF Flexível 450/750V - FICAP" },
                new CableCatalogItem { Familia = "450-750V", SecaoNominal = "6", DiametroExternoMm = 4.62, Descricao = "Noflam Antichama BWF Flexível 450/750V - FICAP" },
                new CableCatalogItem { Familia = "450-750V", SecaoNominal = "10", DiametroExternoMm = 6, Descricao = "Noflam Antichama BWF Flexível 450/750V - FICAP" },
                new CableCatalogItem { Familia = "450-750V", SecaoNominal = "16", DiametroExternoMm = 7, Descricao = "Noflam Antichama BWF Flexível 450/750V - FICAP" },
                new CableCatalogItem { Familia = "450-750V", SecaoNominal = "25", DiametroExternoMm = 8.6, Descricao = "Noflam Antichama BWF Flexível 450/750V - FICAP" },
                new CableCatalogItem { Familia = "450-750V", SecaoNominal = "35", DiametroExternoMm = 10, Descricao = "Noflam Antichama BWF Flexível 450/750V - FICAP" },
                new CableCatalogItem { Familia = "450-750V", SecaoNominal = "50", DiametroExternoMm = 12, Descricao = "Noflam Antichama BWF Flexível 450/750V - FICAP" },
                new CableCatalogItem { Familia = "450-750V", SecaoNominal = "70", DiametroExternoMm = 13.7, Descricao = "Noflam Antichama BWF Flexível 450/750V - FICAP" },
                new CableCatalogItem { Familia = "450-750V", SecaoNominal = "95", DiametroExternoMm = 16, Descricao = "Noflam Antichama BWF Flexível 450/750V - FICAP" },
                new CableCatalogItem { Familia = "450-750V", SecaoNominal = "120", DiametroExternoMm = 18.0, Descricao = "Noflam Antichama BWF Flexível 450/750V - FICAP" },
                new CableCatalogItem { Familia = "450-750V", SecaoNominal = "150", DiametroExternoMm = 19.9, Descricao = "Noflam Antichama BWF Flexível 450/750V - FICAP" },
                new CableCatalogItem { Familia = "450-750V", SecaoNominal = "185", DiametroExternoMm = 22.5, Descricao = "Noflam Antichama BWF Flexível 450/750V - FICAP" },
                new CableCatalogItem { Familia = "450-750V", SecaoNominal = "240", DiametroExternoMm = 24.8, Descricao = "Noflam Antichama BWF Flexível 450/750V - FICAP" },

                // =====================================================
                // 0,6-1kV
                // =====================================================
                new CableCatalogItem { Familia = "0,6-1kV", SecaoNominal = "1,5", DiametroExternoMm = 5.50, Descricao = "AFITOX EP90-F 1kV - NEXANS" },
                new CableCatalogItem { Familia = "0,6-1kV", SecaoNominal = "2,5", DiametroExternoMm = 6.00, Descricao = "AFITOX EP90-F 1kV - NEXANS" },
                new CableCatalogItem { Familia = "0,6-1kV", SecaoNominal = "4", DiametroExternoMm = 6.50, Descricao = "AFITOX EP90-F 1kV - NEXANS" },
                new CableCatalogItem { Familia = "0,6-1kV", SecaoNominal = "6", DiametroExternoMm = 7.00, Descricao = "AFITOX EP90-F 1kV - NEXANS" },
                new CableCatalogItem { Familia = "0,6-1kV", SecaoNominal = "10", DiametroExternoMm = 8.20, Descricao = "AFITOX EP90-F 1kV - NEXANS" },
                new CableCatalogItem { Familia = "0,6-1kV", SecaoNominal = "16", DiametroExternoMm = 9.20, Descricao = "AFITOX EP90-F 1kV - NEXANS" },
                new CableCatalogItem { Familia = "0,6-1kV", SecaoNominal = "25", DiametroExternoMm = 11.50, Descricao = "AFITOX EP90-F 1kV - NEXANS" },
                new CableCatalogItem { Familia = "0,6-1kV", SecaoNominal = "35", DiametroExternoMm = 12.50, Descricao = "AFITOX EP90-F 1kV - NEXANS" },
                new CableCatalogItem { Familia = "0,6-1kV", SecaoNominal = "50", DiametroExternoMm = 14.50, Descricao = "AFITOX EP90-F 1kV - NEXANS" },
                new CableCatalogItem { Familia = "0,6-1kV", SecaoNominal = "70", DiametroExternoMm = 16.50, Descricao = "AFITOX EP90-F 1kV - NEXANS" },
                new CableCatalogItem { Familia = "0,6-1kV", SecaoNominal = "95", DiametroExternoMm = 18.50, Descricao = "AFITOX EP90-F 1kV - NEXANS" },
                new CableCatalogItem { Familia = "0,6-1kV", SecaoNominal = "120", DiametroExternoMm = 20.50, Descricao = "AFITOX EP90-F 1kV - NEXANS" },
                new CableCatalogItem { Familia = "0,6-1kV", SecaoNominal = "150", DiametroExternoMm = 22.50, Descricao = "AFITOX EP90-F 1kV - NEXANS" },
                new CableCatalogItem { Familia = "0,6-1kV", SecaoNominal = "185", DiametroExternoMm = 24.50, Descricao = "AFITOX EP90-F 1kV - NEXANS" },
                new CableCatalogItem { Familia = "0,6-1kV", SecaoNominal = "240", DiametroExternoMm = 28.00, Descricao = "AFITOX EP90-F 1kV - NEXANS" },

                // =====================================================
                // UTP4P
                // =====================================================
                new CableCatalogItem { Familia = "UTP4P", SecaoNominal = "Cat5e", DiametroExternoMm = 4.8, Descricao = "Linha GigaLan - Furukawa" },
                new CableCatalogItem { Familia = "UTP4P", SecaoNominal = "Cat6", DiametroExternoMm = 6.0, Descricao = "Linha GigaLan - Furukawa" },
                new CableCatalogItem { Familia = "UTP4P", SecaoNominal = "Cat6A", DiametroExternoMm = 8.4, Descricao = "Linha GigaLan - Furukawa" },

                // =====================================================
                // Cabo Blindado 2 Vias
                // =====================================================
                new CableCatalogItem { Familia = "Cabo Blindado 2 Vias", SecaoNominal = "1,5", DiametroExternoMm = 8.03, Descricao = "Induscabos Cabo Para Alarme de Incêndio 0.6/1 kV" },
                new CableCatalogItem { Familia = "Cabo Blindado 2 Vias", SecaoNominal = "2,5", DiametroExternoMm = 8.95, Descricao = "Induscabos Cabo Para Alarme de Incêndio 0.6/1 kV" },

                // =====================================================
                // Cabo CC Solar
                // =====================================================
                new CableCatalogItem { Familia = "Cabo CC Solar", SecaoNominal = "1,5", DiametroExternoMm = 5.4, Descricao = "Cabo CC Solar" },
                new CableCatalogItem { Familia = "Cabo CC Solar", SecaoNominal = "2,5", DiametroExternoMm = 5.9, Descricao = "Cabo CC Solar" },
                new CableCatalogItem { Familia = "Cabo CC Solar", SecaoNominal = "4", DiametroExternoMm = 6.6, Descricao = "Cabo CC Solar" },
                new CableCatalogItem { Familia = "Cabo CC Solar", SecaoNominal = "6", DiametroExternoMm = 7.4, Descricao = "Cabo CC Solar" },
                new CableCatalogItem { Familia = "Cabo CC Solar", SecaoNominal = "10", DiametroExternoMm = 8.8, Descricao = "Cabo CC Solar" },
                new CableCatalogItem { Familia = "Cabo CC Solar", SecaoNominal = "16", DiametroExternoMm = 10.1, Descricao = "Cabo CC Solar" },
                new CableCatalogItem { Familia = "Cabo CC Solar", SecaoNominal = "25", DiametroExternoMm = 12.5, Descricao = "Cabo CC Solar" },
                new CableCatalogItem { Familia = "Cabo CC Solar", SecaoNominal = "35", DiametroExternoMm = 14.0, Descricao = "Cabo CC Solar" },
                new CableCatalogItem { Familia = "Cabo CC Solar", SecaoNominal = "50", DiametroExternoMm = 16.3, Descricao = "Cabo CC Solar" },
                new CableCatalogItem { Familia = "Cabo CC Solar", SecaoNominal = "70", DiametroExternoMm = 18.7, Descricao = "Cabo CC Solar" },
                new CableCatalogItem { Familia = "Cabo CC Solar", SecaoNominal = "95", DiametroExternoMm = 20.8, Descricao = "Cabo CC Solar" },
                new CableCatalogItem { Familia = "Cabo CC Solar", SecaoNominal = "120", DiametroExternoMm = 23.0, Descricao = "Cabo CC Solar" },
                new CableCatalogItem { Familia = "Cabo CC Solar", SecaoNominal = "150", DiametroExternoMm = 25.7, Descricao = "Cabo CC Solar" },
                new CableCatalogItem { Familia = "Cabo CC Solar", SecaoNominal = "185", DiametroExternoMm = 28.7, Descricao = "Cabo CC Solar" },
                new CableCatalogItem { Familia = "Cabo CC Solar", SecaoNominal = "244", DiametroExternoMm = 32.3, Descricao = "Cabo CC Solar" },

                // =====================================================
                // Cabo PP 2 Condutores
                // =====================================================
                new CableCatalogItem { Familia = "Cabo PP 2 Condutores", SecaoNominal = "0,5", DiametroExternoMm = 6.1, Descricao = "Cabo PP - 2 Condutores" },
                new CableCatalogItem { Familia = "Cabo PP 2 Condutores", SecaoNominal = "0,75", DiametroExternoMm = 6.5, Descricao = "Cabo PP - 2 Condutores" },
                new CableCatalogItem { Familia = "Cabo PP 2 Condutores", SecaoNominal = "1", DiametroExternoMm = 6.8, Descricao = "Cabo PP - 2 Condutores" },
                new CableCatalogItem { Familia = "Cabo PP 2 Condutores", SecaoNominal = "1,5", DiametroExternoMm = 7.7, Descricao = "Cabo PP - 2 Condutores" },
                new CableCatalogItem { Familia = "Cabo PP 2 Condutores", SecaoNominal = "2,5", DiametroExternoMm = 9.5, Descricao = "Cabo PP - 2 Condutores" },
                new CableCatalogItem { Familia = "Cabo PP 2 Condutores", SecaoNominal = "4", DiametroExternoMm = 10.8, Descricao = "Cabo PP - 2 Condutores" },
                new CableCatalogItem { Familia = "Cabo PP 2 Condutores", SecaoNominal = "6", DiametroExternoMm = 12.3, Descricao = "Cabo PP - 2 Condutores" },
                new CableCatalogItem { Familia = "Cabo PP 2 Condutores", SecaoNominal = "10", DiametroExternoMm = 15.6, Descricao = "Cabo PP - 2 Condutores" },

                // =====================================================
                // 4x0,40mm MC
                // =====================================================
                new CableCatalogItem { Familia = "4x0,40mm MC", SecaoNominal = "0,4", DiametroExternoMm = 0.32, Descricao = "4 x 0,4 mm MC - alarme" }
            };
        }
    }
}