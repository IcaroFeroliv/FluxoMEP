using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Autodesk.Revit.DB;
using AirConditioningClash.Utils;
using View = Autodesk.Revit.DB.View;

namespace AirConditioningClash.Views.HID
{
    public partial class Tag3DHIDConfigView : Window
    {
        private readonly Document _doc;
        private readonly View _activeView;
        public Tag3DHIDSettings Settings { get; private set; }

        public Tag3DHIDConfigView(Document doc, View activeView)
        {
            InitializeComponent();
            _doc = doc;
            _activeView = activeView;
            Settings = Tag3DHIDSettings.Load();

            CarregarFamiliasTags();
            CarregarWorksets();
            ApplySettingsToUI();
        }

        // ───────────────────────────────────────────────────
        // CARGA DE DADOS
        // ───────────────────────────────────────────────────

        private void CarregarFamiliasTags()
        {
            // Em hidrossanitário todas as tags são da categoria OST_PipeTags.
            // O usuário escolhe qual família representa Diâmetro / Inclinação / Sentido.
            var tags = new FilteredElementCollector(_doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_PipeTags)
                .Cast<FamilySymbol>()
                .OrderBy(x => x.Name)
                .ToList();

            // Listas separadas (cópias) para que as três ComboBoxes não compartilhem
            // o mesmo SelectedItem por referência.
            cmbDiametro.ItemsSource = tags.ToList();
            cmbInclinacao.ItemsSource = tags.ToList();
            cmbSentido.ItemsSource = tags.ToList();

            var conexTags = new FilteredElementCollector(_doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_PipeFittingTags)
                .Cast<FamilySymbol>()
                .OrderBy(x => x.Name)
                .ToList();

                    cmbConexao.ItemsSource = conexTags;

        }

        private void CarregarWorksets()
        {
            if (!_doc.IsWorkshared)
            {
                chkFiltrarSistema.IsEnabled = false;
                chkFiltrarSistema.Content = "Filtrar por Workset (projeto não compartilhado)";
                return;
            }

            var worksets = new FilteredWorksetCollector(_doc)
                .OfKind(WorksetKind.UserWorkset)
                .OrderBy(w => w.Name)
                .ToList();

            cmbSistemas.ItemsSource = worksets;
        }

        private void ApplySettingsToUI()
        {
            chkDiametro.IsChecked = Settings.InserirDiametro;
            chkInclinacao.IsChecked = Settings.InserirInclinacao;
            chkSentido.IsChecked = Settings.InserirSentido;
            chkConexao.IsChecked = Settings.InserirConexao;

            SelecionarFamilia(cmbDiametro, Settings.FamiliaDiametro);
            SelecionarFamilia(cmbInclinacao, Settings.FamiliaInclinacao);
            SelecionarFamilia(cmbSentido, Settings.FamiliaSentido);
            SelecionarFamilia(cmbConexao, Settings.FamiliaConexao);

            rbCima.IsChecked = Settings.Posicao != "BaixoDireita";
            rbBaixo.IsChecked = Settings.Posicao == "BaixoDireita";

            txtDistancia.Text = Settings.DistanciaCm.ToString();
            txtComprimentoMin.Text = Settings.ComprimentoMinCm.ToString();
            txtEspacamentoLong.Text = Settings.EspacamentoLongCm.ToString();

            chkFiltrarSistema.IsChecked = Settings.FiltrarPorWorkset && _doc.IsWorkshared;
            cmbSistemas.IsEnabled = chkFiltrarSistema.IsChecked == true;

            if (!string.IsNullOrEmpty(Settings.WorksetSelecionado) &&
                cmbSistemas.ItemsSource is IEnumerable<Workset> ws)
            {
                var sel = ws.FirstOrDefault(w => w.Name == Settings.WorksetSelecionado);
                if (sel != null) cmbSistemas.SelectedItem = sel;
            }

            chkSelecaoManual.IsChecked = Settings.SelecaoManual;
            chkLeader.IsChecked = Settings.HasLeader;

            AtualizarEstadoCombos();
        }

        private void SelecionarFamilia(ComboBox cmb, string nome)
        {
            if (!(cmb.ItemsSource is IEnumerable<FamilySymbol> fams)) return;

            var f = !string.IsNullOrEmpty(nome) ? fams.FirstOrDefault(x => x.Name == nome) : null;
            if (f != null) cmb.SelectedItem = f;
            else if (fams.Any()) cmb.SelectedIndex = 0;
        }

        // ───────────────────────────────────────────────────
        // EVENTOS
        // ───────────────────────────────────────────────────

        private void chk_Checked(object sender, RoutedEventArgs e) => AtualizarEstadoCombos();

        private void AtualizarEstadoCombos()
        {
            // Verifica cada par individualmente para garantir que o elemento já foi carregado na UI
            if (cmbDiametro != null && chkDiametro != null)
                cmbDiametro.IsEnabled = chkDiametro.IsChecked == true;

            if (cmbInclinacao != null && chkInclinacao != null)
                cmbInclinacao.IsEnabled = chkInclinacao.IsChecked == true;

            if (cmbSentido != null && chkSentido != null)
                cmbSentido.IsEnabled = chkSentido.IsChecked == true;

            if (cmbConexao != null && chkConexao != null)
                cmbConexao.IsEnabled = chkConexao.IsChecked == true;
        }

        private void chkFiltrarSistema_Changed(object sender, RoutedEventArgs e)
        {
            // Adicionada a validação do _doc para evitar quebra na inicialização
            if (cmbSistemas == null || _doc == null) return;

            cmbSistemas.IsEnabled = chkFiltrarSistema.IsChecked == true && _doc.IsWorkshared;
        }

        // ───────────────────────────────────────────────────
        // BOTÕES
        // ───────────────────────────────────────────────────

        private void btnExecutar_Click(object sender, RoutedEventArgs e)
        {
            // Validações
            if (chkDiametro.IsChecked != true &&
                chkInclinacao.IsChecked != true &&
                chkSentido.IsChecked != true &&
                chkConexao.IsChecked != true)
            {
                MessageBox.Show("Selecione ao menos uma tag para inserir.");
                return;
            }
            if (chkDiametro.IsChecked == true && cmbDiametro.SelectedItem == null)
            {
                MessageBox.Show("Selecione a família para a tag de Diâmetro.");
                return;
            }
            if (chkInclinacao.IsChecked == true && cmbInclinacao.SelectedItem == null)
            {
                MessageBox.Show("Selecione a família para a tag de Inclinação.");
                return;
            }
            if (chkSentido.IsChecked == true && cmbSentido.SelectedItem == null)
            {
                MessageBox.Show("Selecione a família para a tag de Sentido de Fluxo.");
                return;
            }
            // Validação
            if (chkConexao.IsChecked == true && cmbConexao.SelectedItem == null)
            {
                MessageBox.Show("Selecione a família para a tag de Conexão.");
                return;
            }

            if (chkFiltrarSistema.IsChecked == true && cmbSistemas.SelectedItem == null)
            {
                MessageBox.Show("Selecione um Workset ou desmarque o filtro.");
                return;
            }

            if (!double.TryParse(txtDistancia.Text, out double dist) || dist < 0)
            {
                MessageBox.Show("Distância da tag inválida.");
                return;
            }
            if (!double.TryParse(txtComprimentoMin.Text, out double cmin) || cmin < 0)
            {
                MessageBox.Show("Comprimento mínimo inválido.");
                return;
            }
            if (!double.TryParse(txtEspacamentoLong.Text, out double espac) || espac < 0)
            {
                MessageBox.Show("Espaçamento entre tags inválido.");
                return;
            }

            // Persistência
            Settings.InserirDiametro = chkDiametro.IsChecked == true;
            Settings.InserirInclinacao = chkInclinacao.IsChecked == true;
            Settings.InserirSentido = chkSentido.IsChecked == true;
            Settings.InserirConexao = chkConexao.IsChecked == true;

            Settings.FamiliaDiametro = (cmbDiametro.SelectedItem as FamilySymbol)?.Name ?? "";
            Settings.FamiliaInclinacao = (cmbInclinacao.SelectedItem as FamilySymbol)?.Name ?? "";
            Settings.FamiliaSentido = (cmbSentido.SelectedItem as FamilySymbol)?.Name ?? "";
            Settings.FamiliaConexao = (cmbConexao.SelectedItem as FamilySymbol)?.Name ?? "";

            Settings.Posicao = rbBaixo.IsChecked == true ? "BaixoDireita" : "CimaEsquerda";

            Settings.DistanciaCm = dist;
            Settings.ComprimentoMinCm = cmin;
            Settings.EspacamentoLongCm = espac;

            Settings.FiltrarPorWorkset = chkFiltrarSistema.IsChecked == true && _doc.IsWorkshared;
            Settings.WorksetSelecionado = (cmbSistemas.SelectedItem as Workset)?.Name ?? "";

            Settings.SelecaoManual = chkSelecaoManual.IsChecked == true;
            Settings.HasLeader = chkLeader.IsChecked == true;

            Tag3DHIDSettings.Save(Settings);
            DialogResult = true;
            Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}