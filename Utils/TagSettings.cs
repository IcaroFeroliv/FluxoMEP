using System;
using System.IO;
using System.Xml.Serialization;

namespace AirConditioningClash.Utils
{
    public enum TagPlacement
    {
        Top,    // Acima
        Bottom, // Abaixo
        Right,  // Direita
        Left,    // Esquerda
        Center // Centralizado 
    }

    [Serializable]
    public class TagSettings
    {
        // Seleção
        public bool IsPipeCategory { get; set; } = true;
        public string LastFamilySymbolName { get; set; } = "";
        public bool IsDuctCategory { get; set; } = false; 
        public double MinimumLengthMm { get; set; } = 500;

        // Posições (Direção)
        public TagPlacement HorizontalPipePlacement { get; set; } = TagPlacement.Top;  // Y
        public TagPlacement VerticalPipePlacement { get; set; } = TagPlacement.Right;  // X

        // --- DISTÂNCIAS SEPARADAS ---

        // Distância para tubos que correm na Horizontal (Tag vai p/ Cima/Baixo)
        // Ex: O usuário quer 500mm aqui.
        public double OffsetForHorizontalPipesMm { get; set; } = 500;

        // Distância para tubos que correm na Vertical (Tag vai p/ Dir/Esq)
        // Ex: O usuário quer 1000mm aqui.
        public double OffsetForVerticalPipesMm { get; set; } = 1000;

        // Empilhamento
        public double StackDistanceMm { get; set; } = 150;
        public double MaxPairingDistanceMm { get; set; } = 300;

        // Visual
        public bool HasLeader { get; set; } = true;

        // XML Save/Load Padrão
        public static void Save(TagSettings settings)
        {
            try
            {
                string path = Path.Combine(Path.GetTempPath(), "AirConditioningClash_TagSettings_V2.xml");
                XmlSerializer serializer = new XmlSerializer(typeof(TagSettings));
                using (StreamWriter writer = new StreamWriter(path)) { serializer.Serialize(writer, settings); }
            }
            catch { }
        }

        public static TagSettings Load()
        {
            try
            {
                string path = Path.Combine(Path.GetTempPath(), "AirConditioningClash_TagSettings_V2.xml");
                if (File.Exists(path))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(TagSettings));
                    using (StreamReader reader = new StreamReader(path)) { return (TagSettings)serializer.Deserialize(reader); }
                }
            }
            catch { }
            return new TagSettings();
        }
    }
}