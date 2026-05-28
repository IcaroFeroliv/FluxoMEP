using System.Collections.Generic;
using System.Windows;
using Autodesk.Revit.DB;
using AirConditioningClash.Utils; // Acessa ConfiguracoesGlobais (Climatização)

namespace AirConditioningClash.Views.Climatizacao
{
    public partial class RadarConfigViewHID : Window
    {
        public bool LigarRadar { get; private set; } = false;

        public RadarConfigViewHID()
        {
            InitializeComponent();
            CarregarConfiguracoes();
        }

        private void CarregarConfiguracoes()
        {
            // Carrega da memória GLOBAL (usada para Climatização)
            List<BuiltInCategory> lista = ConfiguracoesGlobais.CategoriasRadar;

            // Estrutura
            if (lista.Contains(BuiltInCategory.OST_StructuralFraming)) chkVigas.IsChecked = true;
            if (lista.Contains(BuiltInCategory.OST_StructuralColumns) || lista.Contains(BuiltInCategory.OST_Columns)) chkPilares.IsChecked = true;
            if (lista.Contains(BuiltInCategory.OST_Floors)) chkLajes.IsChecked = true;

            // Arquitetura
            if (lista.Contains(BuiltInCategory.OST_Walls)) chkParedes.IsChecked = true;
            if (lista.Contains(BuiltInCategory.OST_Ceilings)) chkForros.IsChecked = true;

            // MEP (Dutos e Split/Tubos)
            if (lista.Contains(BuiltInCategory.OST_DuctCurves)) chkDutos.IsChecked = true;
            if (lista.Contains(BuiltInCategory.OST_CableTray)) chkEletrocalhas.IsChecked = true;
            if (lista.Contains(BuiltInCategory.OST_GenericModel)) chkGeneric.IsChecked = true;

            // --- TUBULAÇÃO (Split) ---
            if (lista.Contains(BuiltInCategory.OST_PipeCurves)) chkTubulacao.IsChecked = true;
            if (lista.Contains(BuiltInCategory.OST_PipeFitting)) chkConexoesdeTubo.IsChecked = true;
            if (lista.Contains(BuiltInCategory.OST_PipeAccessory)) chkAcessorios.IsChecked = true;

            // Elétrica
            if (lista.Contains(BuiltInCategory.OST_Conduit)) chkConduites.IsChecked = true;
            if (lista.Contains(BuiltInCategory.OST_ConduitFitting)) chkConexao.IsChecked = true;
            if (lista.Contains(BuiltInCategory.OST_LightingFixtures)) chkluminarias.IsChecked = true;
        }

        private void btnSalvar_Click(object sender, RoutedEventArgs e)
        {
            List<BuiltInCategory> novaLista = new List<BuiltInCategory>();

            // Estrutura
            if (chkVigas.IsChecked == true) novaLista.Add(BuiltInCategory.OST_StructuralFraming);
            if (chkPilares.IsChecked == true)
            {
                novaLista.Add(BuiltInCategory.OST_StructuralColumns);
                novaLista.Add(BuiltInCategory.OST_Columns);
            }
            if (chkLajes.IsChecked == true) novaLista.Add(BuiltInCategory.OST_Floors);

            // Arquitetura
            if (chkParedes.IsChecked == true) novaLista.Add(BuiltInCategory.OST_Walls);
            if (chkForros.IsChecked == true) novaLista.Add(BuiltInCategory.OST_Ceilings);

            // MEP
            if (chkDutos.IsChecked == true) novaLista.Add(BuiltInCategory.OST_DuctCurves);
            if (chkEletrocalhas.IsChecked == true) novaLista.Add(BuiltInCategory.OST_CableTray);
            if (chkGeneric.IsChecked == true) novaLista.Add(BuiltInCategory.OST_GenericModel);

            // --- TUBULAÇÃO (Split) ---
            if (chkTubulacao.IsChecked == true) novaLista.Add(BuiltInCategory.OST_PipeCurves);
            if (chkConexoesdeTubo.IsChecked == true) novaLista.Add(BuiltInCategory.OST_PipeFitting);
            if (chkAcessorios.IsChecked == true) novaLista.Add(BuiltInCategory.OST_PipeAccessory);

            // Elétrica
            if (chkConduites.IsChecked == true) novaLista.Add(BuiltInCategory.OST_Conduit);
            if (chkConexao.IsChecked == true) novaLista.Add(BuiltInCategory.OST_ConduitFitting);
            if (chkluminarias.IsChecked == true) novaLista.Add(BuiltInCategory.OST_LightingFixtures);

            // Salva na memória GLOBAL
            ConfiguracoesGlobais.CategoriasRadar = novaLista;

            LigarRadar = true;
            this.Close();
        }

        private void btnDesligar_Click(object sender, RoutedEventArgs e)
        {
            LigarRadar = false;
            this.Close();
        }
    }
}