using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Autodesk.Revit.DB;

namespace AirConditioningClash.Views.HID
{
    public class SimpleOption
    {
        public string Nome { get; set; }
        public ElementId Id { get; set; }
        public WorksetId IdWorkset { get; set; }
        public override string ToString() => Nome;
    }

    public partial class DetalhamentoView : Window
    {
        private TagSettingsHDS _settings;

        public bool InserirDiametro => chkDiametro.IsChecked == true;
        public bool InserirInclinacao => chkInclinacao.IsChecked == true;
        public bool InserirSentido => chkSentido.IsChecked == true;
        public bool PreferenciaCimaEsquerda => rbCima.IsChecked == true;
        public bool FiltrarPorWorkset => chkFiltrarSistema.IsChecked == true;
        public bool InserirConexao => chkConexao.IsChecked == true;
        public bool HasLeader => chkLeader.IsChecked == true;

        public double DistanciaCm { get; private set; }
        public double ComprimentoMinimoCm { get; private set; }
        public double EspacamentoLongitudinalCm { get; private set; }
        

        public ElementId IdTagDiametroSelecionada { get; private set; }
        public ElementId IdTagInclinacaoSelecionada { get; private set; }
        public ElementId IdTagSentidoSelecionada { get; private set; }
        public ElementId IdTagConexaoSelecionada { get; private set; }
        public string NomeWorksetEscolhido { get; private set; }
        public bool UsarSelecaoManual { get; private set; }

        public DetalhamentoView(List<FamilySymbol> tagsTubo, List<FamilySymbol> tagsConexao, IList<Workset> worksets)
        {
            InitializeComponent();
            _settings = TagSettingsHDS.Load();
            CarregarTags(tagsTubo, tagsConexao);
            CarregarWorksets(worksets);
            AplicarMemoria();
        }

        private void CarregarTags(List<FamilySymbol> tagsTubo, List<FamilySymbol> tagsConexao)
        {
            var opcoesTubo = tagsTubo.Select(t => new SimpleOption { Nome = $"{t.FamilyName} : {t.Name}", Id = t.Id })
                             .OrderBy(x => x.Nome).ToList();

            var opcoesConexao = tagsConexao.Select(t => new SimpleOption { Nome = $"{t.FamilyName} : {t.Name}", Id = t.Id })
                             .OrderBy(x => x.Nome).ToList();

            cmbDiametro.ItemsSource = opcoesTubo;
            cmbInclinacao.ItemsSource = opcoesTubo;
            cmbSentido.ItemsSource = opcoesTubo;
            cmbConexao.ItemsSource = opcoesConexao;
        }

        private void CarregarWorksets(IList<Workset> worksets)
        {
            var opcoes = worksets.Where(w => w.Kind == WorksetKind.UserWorkset)
                .Select(w => new SimpleOption { Nome = w.Name, IdWorkset = w.Id })
                .OrderBy(x => x.Nome).ToList();
            cmbSistemas.ItemsSource = opcoes;
        }

        private void AplicarMemoria()
        {
            chkDiametro.IsChecked = _settings.InserirDiametro;
            chkInclinacao.IsChecked = _settings.InserirInclinacao;
            chkSentido.IsChecked = _settings.InserirSentido;
            rbCima.IsChecked = _settings.PreferenciaCimaEsquerda;
            rbBaixo.IsChecked = !_settings.PreferenciaCimaEsquerda;
            chkFiltrarSistema.IsChecked = _settings.FiltrarPorWorkset;
            chkConexao.IsChecked = _settings.InserirConexao;

            txtDistancia.Text = _settings.DistanciaDoTuboCm.ToString();
            txtComprimentoMin.Text = _settings.ComprimentoMinimoCm.ToString();
            txtEspacamentoLong.Text = _settings.DistanciaEntreTagsLongitudinalCm.ToString();
            chkSelecaoManual.IsChecked = _settings.UsarSelecaoManual; 
            chkLeader.IsChecked = _settings.HasLeader;

            if (!string.IsNullOrEmpty(_settings.NomeFamiliaDiametro))
                SelecionarPorNome(cmbDiametro, _settings.NomeFamiliaDiametro);
            if (!string.IsNullOrEmpty(_settings.NomeFamiliaInclinacao))
                SelecionarPorNome(cmbInclinacao, _settings.NomeFamiliaInclinacao);
            if (!string.IsNullOrEmpty(_settings.NomeFamiliaSentido))
                SelecionarPorNome(cmbSentido, _settings.NomeFamiliaSentido);
            if (!string.IsNullOrEmpty(_settings.NomeWorksetSelecionado))
                SelecionarPorNome(cmbSistemas, _settings.NomeWorksetSelecionado);
            if (!string.IsNullOrEmpty(_settings.NomeFamiliaConexao))
                SelecionarPorNome(cmbConexao, _settings.NomeFamiliaConexao);
        }

        private void SelecionarPorNome(System.Windows.Controls.ComboBox cmb, string nomeParcial)
        {
            foreach (SimpleOption item in cmb.ItemsSource)
            {
                if (item.Nome.Equals(nomeParcial, System.StringComparison.InvariantCultureIgnoreCase))
                {
                    cmb.SelectedItem = item;
                    break;
                }
            }
        }

        private void btnExecutar_Click(object sender, RoutedEventArgs e)
        {
            if (chkFiltrarSistema.IsChecked == true && cmbSistemas.SelectedItem == null)
            {
                MessageBox.Show("Selecione um Workset para filtrar.");
                return;
            }

            if (!double.TryParse(txtDistancia.Text, out double distVal) || distVal < 0)
            {
                MessageBox.Show("Distância inválida."); return;
            }
            if (!double.TryParse(txtComprimentoMin.Text, out double compVal) || compVal < 0)
            {
                MessageBox.Show("Comprimento inválido."); return;
            }

            // CORREÇÃO: Aceita negativos agora!
            if (!double.TryParse(txtEspacamentoLong.Text, out double espacamentoVal))
            {
                MessageBox.Show("Espaçamento inválido."); return;
            }

            DistanciaCm = distVal;
            ComprimentoMinimoCm = compVal;
            EspacamentoLongitudinalCm = espacamentoVal;

            if (cmbDiametro.SelectedItem != null) IdTagDiametroSelecionada = (cmbDiametro.SelectedItem as SimpleOption).Id;
            if (cmbInclinacao.SelectedItem != null) IdTagInclinacaoSelecionada = (cmbInclinacao.SelectedItem as SimpleOption).Id;
            if (cmbSentido.SelectedItem != null) IdTagSentidoSelecionada = (cmbSentido.SelectedItem as SimpleOption).Id;
            if (cmbSistemas.SelectedItem != null) NomeWorksetEscolhido = (cmbSistemas.SelectedItem as SimpleOption).Nome;

            _settings.InserirDiametro = chkDiametro.IsChecked == true;
            _settings.InserirInclinacao = chkInclinacao.IsChecked == true;
            _settings.InserirSentido = chkSentido.IsChecked == true;
            _settings.PreferenciaCimaEsquerda = rbCima.IsChecked == true;
            _settings.FiltrarPorWorkset = chkFiltrarSistema.IsChecked == true;
            UsarSelecaoManual = chkSelecaoManual.IsChecked == true;
            _settings.DistanciaDoTuboCm = distVal;
            _settings.ComprimentoMinimoCm = compVal;
            _settings.DistanciaEntreTagsLongitudinalCm = espacamentoVal;
            _settings.UsarSelecaoManual = UsarSelecaoManual;
            _settings.HasLeader = HasLeader;

            if (cmbDiametro.SelectedItem != null) _settings.NomeFamiliaDiametro = (cmbDiametro.SelectedItem as SimpleOption).Nome;
            if (cmbInclinacao.SelectedItem != null) _settings.NomeFamiliaInclinacao = (cmbInclinacao.SelectedItem as SimpleOption).Nome;
            if (cmbSentido.SelectedItem != null) _settings.NomeFamiliaSentido = (cmbSentido.SelectedItem as SimpleOption).Nome;
            if (cmbSistemas.SelectedItem != null) _settings.NomeWorksetSelecionado = (cmbSistemas.SelectedItem as SimpleOption).Nome;

            if (cmbConexao.SelectedItem != null) IdTagConexaoSelecionada = (cmbConexao.SelectedItem as SimpleOption).Id;
            _settings.InserirConexao = chkConexao.IsChecked == true;
            if (cmbConexao.SelectedItem != null) _settings.NomeFamiliaConexao = (cmbConexao.SelectedItem as SimpleOption).Nome;

            _settings.Save();
            this.DialogResult = true;
            this.Close();
        }

        
        private void chkFiltrarSistema_Changed(object sender, RoutedEventArgs e) { if (cmbSistemas != null) cmbSistemas.IsEnabled = chkFiltrarSistema.IsChecked == true; }
        private void chk_Checked(object sender, RoutedEventArgs e)
        {
            if (cmbDiametro != null) cmbDiametro.IsEnabled = chkDiametro.IsChecked == true;
            if (cmbInclinacao != null) cmbInclinacao.IsEnabled = chkInclinacao.IsChecked == true;
            if (cmbSentido != null) cmbSentido.IsEnabled = chkSentido.IsChecked == true;
            if (cmbConexao != null) cmbConexao.IsEnabled = chkConexao.IsChecked == true; 
        }
    }
}