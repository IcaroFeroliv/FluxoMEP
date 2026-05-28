using System.Windows;

namespace AirConditioningClash.Views
{
    public enum TipoConflito { Estrutural, Eletrica, Hidrossanitario }

    public partial class ConflictTypeSelectionView : Window
    {
        public TipoConflito TipoSelecionado { get; private set; }
        public bool Confirmado { get; private set; } = false;

        public ConflictTypeSelectionView()
        {
            InitializeComponent();
        }

        private void btnProximo_Click(object sender, RoutedEventArgs e)
        {
            if (rbEstrutural.IsChecked == true)
                TipoSelecionado = TipoConflito.Estrutural;
            else if (rbEletrica.IsChecked == true)
                TipoSelecionado = TipoConflito.Eletrica;
            else
                TipoSelecionado = TipoConflito.Hidrossanitario;

            Confirmado = true;
            this.Close();
        }

        private void btnCancelar_Click(object sender, RoutedEventArgs e)
        {
            Confirmado = false;
            this.Close();
        }
    }
}