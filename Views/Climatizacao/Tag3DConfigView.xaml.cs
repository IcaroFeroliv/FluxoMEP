using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using AirConditioningClash.Utils;
using View = Autodesk.Revit.DB.View;
using Visibility = System.Windows.Visibility;

namespace AirConditioningClash.Views.Climatizacao
{
    public partial class Tag3DConfigView : Window
    {
        private readonly Document _doc;
        private readonly View _activeView;
        public Tag3DSettings Settings { get; private set; }

        public Tag3DConfigView(Document doc, View activeView)
        {
            InitializeComponent();
            _doc = doc;
            _activeView = activeView;
            Settings = Tag3DSettings.Load();
            ApplySettingsToUI();
            LoadFamilies();
            CarregarTiposTubo();
        }

        private void ApplySettingsToUI()
        {
            rbPipes.IsChecked     = Settings.IsPipeCategory;
            rbDucts.IsChecked     = Settings.IsDuctCategory;
            rbEquipment.IsChecked = Settings.IsEquipmentCategory;

            if (!Settings.IsPipeCategory && !Settings.IsDuctCategory && !Settings.IsEquipmentCategory)
                rbPipes.IsChecked = true;

            rbAcima.IsChecked  = Settings.TagPosicaoVertical != "Abaixo";
            rbAbaixo.IsChecked = Settings.TagPosicaoVertical == "Abaixo";

            rbCentro.IsChecked   = Settings.TagPosicaoHorizontal != "Direita" && Settings.TagPosicaoHorizontal != "Esquerda";
            rbDireita.IsChecked  = Settings.TagPosicaoHorizontal == "Direita";
            rbEsquerda.IsChecked = Settings.TagPosicaoHorizontal == "Esquerda";

            txtOffset.Text      = Settings.OffsetMm.ToString();
            txtOffsetH.Text     = Settings.OffsetHorizontalMm.ToString();
            txtMinLength.Text   = Settings.MinimumLengthMm.ToString();
            chkLeader.IsChecked = Settings.HasLeader;

            AtualizarEstadoOffsetH();
        }

        private void OnCategoryChanged(object sender, RoutedEventArgs e)
        {
            if (_doc == null) return;
            LoadFamilies();
            CarregarTiposTubo();
        }

        private void OnHorizontalPositionChanged(object sender, RoutedEventArgs e) => AtualizarEstadoOffsetH();

        private void AtualizarEstadoOffsetH()
        {
            if (txtOffsetH == null) return;
            txtOffsetH.IsEnabled = rbDireita.IsChecked == true || rbEsquerda.IsChecked == true;
        }

        private void CarregarTiposTubo()
        {
            if (_activeView == null || rbPipes.IsChecked != true)
            {
                grpFiltroTubo.Visibility = Visibility.Collapsed;
                return;
            }

            var pipes = new FilteredElementCollector(_doc, _activeView.Id)
                .OfClass(typeof(Pipe))
                .Cast<Pipe>()
                .ToList();

            var tipos = pipes
                .Where(p => p.PipeType != null)
                .GroupBy(p => p.PipeType.Id.Value)
                .Select(g => g.First().PipeType)
                .OrderBy(t => t.Name)
                .ToList();

            if (tipos.Count == 0)
            {
                grpFiltroTubo.Visibility = Visibility.Collapsed;
                return;
            }

            var itens = tipos.Select(t => new TipoTuboItem
            {
                Nome = t.Name,
                Selecionado = Settings.TiposTuboSelecionados.Count == 0 ||
                              Settings.TiposTuboSelecionados.Contains(t.Name)
            }).ToList();

            lstTiposTubo.ItemsSource = itens;
            grpFiltroTubo.Visibility = Visibility.Visible;
        }

        private void btnTodosTubo_Click(object sender, RoutedEventArgs e)
        {
            var itens = lstTiposTubo.ItemsSource as List<TipoTuboItem>;
            if (itens == null) return;
            itens.ForEach(i => i.Selecionado = true);
            lstTiposTubo.ItemsSource = null;
            lstTiposTubo.ItemsSource = itens;
        }

        private void btnNenhumTubo_Click(object sender, RoutedEventArgs e)
        {
            var itens = lstTiposTubo.ItemsSource as List<TipoTuboItem>;
            if (itens == null) return;
            itens.ForEach(i => i.Selecionado = false);
            lstTiposTubo.ItemsSource = null;
            lstTiposTubo.ItemsSource = itens;
        }

        private void LoadFamilies()
        {
            BuiltInCategory tagCategory;
            if (rbEquipment.IsChecked == true)
                tagCategory = BuiltInCategory.OST_MechanicalEquipmentTags;
            else if (rbDucts.IsChecked == true)
                tagCategory = BuiltInCategory.OST_DuctTags;
            else
                tagCategory = BuiltInCategory.OST_PipeTags;

            var symbols = new FilteredElementCollector(_doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(tagCategory)
                .Cast<FamilySymbol>()
                .OrderBy(x => x.Name)
                .ToList();

            cmbTagFamilies.ItemsSource = symbols;

            if (!string.IsNullOrEmpty(Settings.LastFamilySymbolName))
            {
                var lastUsed = symbols.FirstOrDefault(x => x.Name == Settings.LastFamilySymbolName);
                cmbTagFamilies.SelectedItem = lastUsed ?? (symbols.Count > 0 ? (object)symbols[0] : null);
            }
            else if (symbols.Count > 0)
            {
                cmbTagFamilies.SelectedIndex = 0;
            }
        }

        private void btnApply_Click(object sender, RoutedEventArgs e)
        {
            if (cmbTagFamilies.SelectedItem == null)
            {
                MessageBox.Show("Selecione uma família de tag.");
                return;
            }
            if (!double.TryParse(txtOffset.Text, out double offset) || offset < 0)
            {
                MessageBox.Show("Offset vertical inválido.");
                return;
            }
            if (txtOffsetH.IsEnabled && (!double.TryParse(txtOffsetH.Text, out double offsetH) || offsetH < 0))
            {
                MessageBox.Show("Offset horizontal inválido.");
                return;
            }
            if (!double.TryParse(txtMinLength.Text, out double minLen) || minLen < 0)
            {
                MessageBox.Show("Comprimento mínimo inválido.");
                return;
            }

            Settings.IsPipeCategory      = rbPipes.IsChecked == true;
            Settings.IsDuctCategory      = rbDucts.IsChecked == true;
            Settings.IsEquipmentCategory = rbEquipment.IsChecked == true;
            Settings.HasLeader           = chkLeader.IsChecked == true;
            Settings.OffsetMm            = offset;
            Settings.MinimumLengthMm     = minLen;
            Settings.LastFamilySymbolName = ((FamilySymbol)cmbTagFamilies.SelectedItem).Name;

            Settings.TagPosicaoVertical   = rbAbaixo.IsChecked == true ? "Abaixo" : "Acima";
            Settings.TagPosicaoHorizontal = rbDireita.IsChecked == true ? "Direita"
                                          : rbEsquerda.IsChecked == true ? "Esquerda"
                                          : "Centro";
            if (double.TryParse(txtOffsetH.Text, out double parsedH) && parsedH >= 0)
                Settings.OffsetHorizontalMm = parsedH;

            // Salva filtro de tipos de tubo
            if (grpFiltroTubo.Visibility == Visibility.Visible &&
                lstTiposTubo.ItemsSource is List<TipoTuboItem> tipoItens)
            {
                var selecionados = tipoItens.Where(i => i.Selecionado).Select(i => i.Nome).ToList();
                // Lista vazia = sem filtro (todos selecionados)
                Settings.TiposTuboSelecionados = selecionados.Count == tipoItens.Count
                    ? new List<string>()
                    : selecionados;
            }
            else
            {
                Settings.TiposTuboSelecionados = new List<string>();
            }

            Tag3DSettings.Save(Settings);
            DialogResult = true;
            Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }

    internal class TipoTuboItem
    {
        public string Nome { get; set; }
        public bool Selecionado { get; set; }
    }
}
