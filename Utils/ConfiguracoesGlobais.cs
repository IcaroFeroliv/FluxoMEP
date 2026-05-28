using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AirConditioningClash.Utils
{
    // Essa classe estática guarda os valores enquanto o Revit estiver aberto
    public static class ConfiguracoesGlobais
    {
        // ... (Suas variáveis de Direção/Margem continuam aqui) ...
        public static AirConditioningClash.Views.Climatizacao.Direcao UltimaDirecao { get; set; } = AirConditioningClash.Views.Climatizacao.Direcao.Cima;
        public static double UltimaMargemLateral { get; set; } = 10.0;
        public static double UltimaFolga { get; set; } = 5.0;

        // --- NOVA CONFIGURAÇÃO DO RADAR ---
        // Lista padrão: Começa vigiando Vigas e Pilares
        public static List<BuiltInCategory> CategoriasRadar { get; set; } = new List<BuiltInCategory>()
        {
            BuiltInCategory.OST_StructuralFraming,
            BuiltInCategory.OST_StructuralColumns,
            BuiltInCategory.OST_Columns
        };
    }
}
