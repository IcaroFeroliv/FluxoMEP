using System;
using System.Windows;

namespace AirConditioningClash.Views.Climatizacao
{
    public enum Direcao { Cima, Baixo, Esquerda, Direita }

    public partial class ClashResolverView : Window
    {
        public bool Confirmado { get; private set; } = false;
        public Direcao DirecaoEscolhida { get; private set; }
        public double MargemLateralCm { get; private set; }
        public double FolgaCm { get; private set; }

        // MUDANÇA NO CONSTRUTOR: Agora recebemos os valores salvos
        public ClashResolverView(string infoViga, string infoTubo,
                                 Direcao dirSalva, double margemSalva, double folgaSalva)
        {
            InitializeComponent();

            txtViga.Text = infoViga;
            txtTubo.Text = infoTubo;

            // --- RESTAURAR MEMÓRIA NA TELA ---

            // 1. Restaurar Textos
            inputMargem.Text = margemSalva.ToString();
            inputFolga.Text = folgaSalva.ToString();

            // 2. Restaurar Botão de Direção
            switch (dirSalva)
            {
                case Direcao.Cima: rbCima.IsChecked = true; break;
                case Direcao.Baixo: rbBaixo.IsChecked = true; break;
                case Direcao.Esquerda: rbEsquerda.IsChecked = true; break;
                case Direcao.Direita: rbDireita.IsChecked = true; break;
            }
        }

        private void btnAplicar_Click(object sender, RoutedEventArgs e)
        {
            Confirmado = true;

            // Ler Direção
            if (rbCima.IsChecked == true) DirecaoEscolhida = Direcao.Cima;
            else if (rbBaixo.IsChecked == true) DirecaoEscolhida = Direcao.Baixo;
            else if (rbEsquerda.IsChecked == true) DirecaoEscolhida = Direcao.Esquerda;
            else DirecaoEscolhida = Direcao.Direita;

            // Ler Valores
            if (double.TryParse(inputMargem.Text, out double valMargem)) MargemLateralCm = valMargem;
            else MargemLateralCm = 10; // Fallback se o usuário digitar texto errado

            if (double.TryParse(inputFolga.Text, out double valFolga)) FolgaCm = valFolga;
            else FolgaCm = 5;

            this.Close();
        }

        private void btnCancelar_Click(object sender, RoutedEventArgs e)
        {
            Confirmado = false;
            this.Close();
        }
    }
}