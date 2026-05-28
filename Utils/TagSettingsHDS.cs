using System;
using System.IO;
using System.Xml.Serialization;

namespace AirConditioningClash
{
    public class TagSettingsHDS
    {
        public bool InserirDiametro { get; set; } = true;
        public bool InserirInclinacao { get; set; } = true;
        public bool PreferenciaCimaEsquerda { get; set; } = true;
        public bool InserirSentido { get; set; } = false;

        public bool HasLeader { get; set; } = false; 

        public bool InserirConexao { get; set; } = false;

        public string NomeFamiliaSentido { get; set; } = "";

        public string NomeFamiliaDiametro { get; set; } = "";
        public string NomeFamiliaInclinacao { get; set; } = "";
        public string NomeFamiliaConexao { get; set; } = "";

        public bool FiltrarPorWorkset { get; set; } = false;
        public string NomeWorksetSelecionado { get; set; } = "";

        public double DistanciaDoTuboCm { get; set; } = 30.0; 
        public double ComprimentoMinimoCm { get; set; } = 75.0; 
        public double DistanciaEntreTagsLongitudinalCm { get; set; } = 0.0;

        public bool UsarSelecaoManual { get; set; } = false;

        private static string CaminhoArquivo = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "RevitPlugins",
            "TagSettingsHDS.xml");

        public void Save()
        {
            try
            {
                string pasta = Path.GetDirectoryName(CaminhoArquivo);
                if (!Directory.Exists(pasta)) Directory.CreateDirectory(pasta);
                XmlSerializer serializer = new XmlSerializer(typeof(TagSettingsHDS));
                using (StreamWriter writer = new StreamWriter(CaminhoArquivo))
                {
                    serializer.Serialize(writer, this);
                }
            }
            catch { }
        }

        public static TagSettingsHDS Load()
        {
            try
            {
                if (File.Exists(CaminhoArquivo))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(TagSettingsHDS));
                    using (StreamReader reader = new StreamReader(CaminhoArquivo))
                    {
                        return (TagSettingsHDS)serializer.Deserialize(reader);
                    }
                }
            }
            catch { }
            return new TagSettingsHDS();
        }
    }
}