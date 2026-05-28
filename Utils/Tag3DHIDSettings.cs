using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace AirConditioningClash.Utils
{
    [Serializable]
    public class Tag3DHIDSettings
    {
        // Quais tags inserir
        public bool InserirDiametro { get; set; } = true;
        public bool InserirInclinacao { get; set; } = true;
        public bool InserirSentido { get; set; } = false;
        public bool InserirConexao { get; set; } = false;

        // Famílias selecionadas para cada tipo de tag
        public string FamiliaDiametro { get; set; } = "";
        public string FamiliaInclinacao { get; set; } = "";
        public string FamiliaSentido { get; set; } = "";
        public string FamiliaConexao { get; set; } = "";

        // Posição (usada quando há apenas 1 tag selecionada,
        // ou como "lado preferencial" no caso de várias tags)
        // Valores: "CimaEsquerda" | "BaixoDireita"
        public string Posicao { get; set; } = "CimaEsquerda";

        // Ajustes geométricos em centímetros (UI usa cm para HID)
        public double DistanciaCm { get; set; } = 30;       // Distância da tag até o tubo (offset perpendicular)
        public double ComprimentoMinCm { get; set; } = 75;  // Ignora tubos menores que isso
        public double EspacamentoLongCm { get; set; } = 0;  // Espaçamento longitudinal entre tags do mesmo tubo

        // Filtro por Workset
        public bool FiltrarPorWorkset { get; set; } = false;
        public string WorksetSelecionado { get; set; } = "";

        // Outros
        public bool HasLeader { get; set; } = true;
        public bool SelecaoManual { get; set; } = false;

        private static string FilePath =>
            Path.Combine(Path.GetTempPath(), "FluxoMEP_Tag3DHIDSettings.xml");

        public static void Save(Tag3DHIDSettings settings)
        {
            try
            {
                var serializer = new XmlSerializer(typeof(Tag3DHIDSettings));
                using (var writer = new StreamWriter(FilePath))
                    serializer.Serialize(writer, settings);
            }
            catch { }
        }

        public static Tag3DHIDSettings Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    var serializer = new XmlSerializer(typeof(Tag3DHIDSettings));
                    using (var reader = new StreamReader(FilePath))
                        return (Tag3DHIDSettings)serializer.Deserialize(reader);
                }
            }
            catch { }
            return new Tag3DHIDSettings();
        }
    }
}