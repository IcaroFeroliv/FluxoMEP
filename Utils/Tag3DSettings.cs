using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace AirConditioningClash.Utils
{
    [Serializable]
    public class Tag3DSettings
    {
        public bool IsPipeCategory { get; set; } = true;
        public bool IsDuctCategory { get; set; } = false;
        public bool IsEquipmentCategory { get; set; } = false;
        public string LastFamilySymbolName { get; set; } = "";
        public double OffsetMm { get; set; } = 500;
        public double MinimumLengthMm { get; set; } = 500;
        public bool HasLeader { get; set; } = true;
        public string TagPosicaoVertical { get; set; } = "Acima";      // "Acima" | "Abaixo"
        public string TagPosicaoHorizontal { get; set; } = "Centro";   // "Centro" | "Direita" | "Esquerda"
        public double OffsetHorizontalMm { get; set; } = 300;
        // Lista de nomes de PipeType selecionados; vazia = todos os tipos
        public List<string> TiposTuboSelecionados { get; set; } = new List<string>();

        private static string FilePath =>
            Path.Combine(Path.GetTempPath(), "FluxoMEP_Tag3DSettings.xml");

        public static void Save(Tag3DSettings settings)
        {
            try
            {
                var serializer = new XmlSerializer(typeof(Tag3DSettings));
                using (var writer = new StreamWriter(FilePath))
                    serializer.Serialize(writer, settings);
            }
            catch { }
        }

        public static Tag3DSettings Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    var serializer = new XmlSerializer(typeof(Tag3DSettings));
                    using (var reader = new StreamReader(FilePath))
                        return (Tag3DSettings)serializer.Deserialize(reader);
                }
            }
            catch { }
            return new Tag3DSettings();
        }
    }
}
