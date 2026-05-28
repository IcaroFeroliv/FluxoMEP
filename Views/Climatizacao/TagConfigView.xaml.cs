using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using AirConditioningClash.Utils;

namespace AirConditioningClash.Views.Climatizacao
{
    public partial class TagConfigView : Window
    {
        private Document _doc;
        public TagSettings Settings { get; private set; }

        public TagConfigView(Document doc)
        {
            InitializeComponent();
            _doc = doc;
            Settings = TagSettings.Load(); // Carrega as configurações salvas

            // 1. Aplica as configurações visuais salvas nos campos
            ApplySettingsToUI();

            // 2. Carrega a lista de famílias correta (Tubos ou Equipamentos)
            LoadFamilies();
        }

        private void ApplySettingsToUI()
        {
            // Define Categoria (Tubo ou Equipamento)
            if (Settings.IsPipeCategory)
                rbPipes.IsChecked = true;
            else
                rbEquipment.IsChecked = true;

            // --- NOVOS CAMPOS DE DISTÂNCIA SEPARADOS ---
            txtOffsetHoriz.Text = Settings.OffsetForHorizontalPipesMm.ToString();
            txtOffsetVert.Text = Settings.OffsetForVerticalPipesMm.ToString();

            // Campo de Comprimento Mínimo
            txtMinLength.Text = Settings.MinimumLengthMm.ToString();

            // Outros campos numéricos
            txtStack.Text = Settings.StackDistanceMm.ToString();

            // Opções visuais
            chkLeader.IsChecked = Settings.HasLeader;

            // Posição Horizontal (Y)
            if (Settings.HorizontalPipePlacement == TagPlacement.Top) rbTop.IsChecked = true;
            else if (Settings.HorizontalPipePlacement == TagPlacement.Center) rbCenterHoriz.IsChecked = true;
            else rbBottom.IsChecked = true;

            // Posição Vertical (X)
            if (Settings.VerticalPipePlacement == TagPlacement.Right) rbRight.IsChecked = true;
            else if (Settings.VerticalPipePlacement == TagPlacement.Center) rbCenterVert.IsChecked = true;
            else rbLeft.IsChecked = true;
        }

        // Evento disparado quando troca a seleção entre Tubo e Equipamento
        private void OnCategoryChanged(object sender, RoutedEventArgs e)
        {
            // Evita erro se rodar antes da janela estar totalmente carregada
            if (_doc == null) return;
            LoadFamilies();
        }

        private void LoadFamilies()
        {
            BuiltInCategory targetCategory = BuiltInCategory.OST_PipeTags;
            if (rbEquipment.IsChecked == true) targetCategory = BuiltInCategory.OST_MechanicalEquipmentTags;
            if (rbDucts.IsChecked == true) targetCategory = BuiltInCategory.OST_DuctTags; // Adicionado

            // Busca os Símbolos (Tipos de Tag) carregados no projeto
            var collector = new FilteredElementCollector(_doc)
                            .OfClass(typeof(FamilySymbol))
                            .OfCategory(targetCategory)
                            .Cast<FamilySymbol>()
                            .OrderBy(x => x.Name)
                            .ToList();

            cmbTagFamilies.ItemsSource = collector;

            // Tenta selecionar a última família usada pelo usuário
            if (!string.IsNullOrEmpty(Settings.LastFamilySymbolName))
            {
                var lastUsed = collector.FirstOrDefault(x => x.Name == Settings.LastFamilySymbolName);
                if (lastUsed != null)
                    cmbTagFamilies.SelectedItem = lastUsed;
                else if (collector.Count > 0)
                    cmbTagFamilies.SelectedIndex = 0;
            }
            else if (collector.Count > 0)
            {
                cmbTagFamilies.SelectedIndex = 0;
            }
        }

        private void btnApply_Click(object sender, RoutedEventArgs e)
        {
            // 1. Salva a Família selecionada
            if (cmbTagFamilies.SelectedItem is FamilySymbol fs)
                Settings.LastFamilySymbolName = fs.Name;

            // 2. Define as categorias
            Settings.IsPipeCategory = (rbPipes.IsChecked == true);
            Settings.IsDuctCategory = (rbDucts.IsChecked == true);

            // 3. Salva o Comprimento Mínimo
            if (double.TryParse(txtMinLength.Text, out double minLen))
                Settings.MinimumLengthMm = minLen;

            // 4. Salva opções visuais
            Settings.HasLeader = chkLeader.IsChecked == true;

            // 5. Salva as Posições (Enums) - AGORA COM 3 OPÇÕES CADA
            // Posição Horizontal (Eixo Y: Acima, Centro ou Baixo)
            if (rbTop.IsChecked == true)
                Settings.HorizontalPipePlacement = TagPlacement.Top;
            else if (rbCenterHoriz.IsChecked == true)
                Settings.HorizontalPipePlacement = TagPlacement.Center;
            else
                Settings.HorizontalPipePlacement = TagPlacement.Bottom;

            // Posição Vertical (Eixo X: Direita, Centro ou Esquerda)
            if (rbRight.IsChecked == true)
                Settings.VerticalPipePlacement = TagPlacement.Right;
            else if (rbCenterVert.IsChecked == true)
                Settings.VerticalPipePlacement = TagPlacement.Center;
            else
                Settings.VerticalPipePlacement = TagPlacement.Left;

            // 6. Salva os Valores Numéricos
            if (double.TryParse(txtOffsetHoriz.Text, out double offH))
                Settings.OffsetForHorizontalPipesMm = offH;

            if (double.TryParse(txtOffsetVert.Text, out double offV))
                Settings.OffsetForVerticalPipesMm = offV;

            if (double.TryParse(txtStack.Text, out double stack))
                Settings.StackDistanceMm = stack;


            // 7. Persiste no arquivo XML para a próxima vez
            TagSettings.Save(Settings);

            this.DialogResult = true;
            this.Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}