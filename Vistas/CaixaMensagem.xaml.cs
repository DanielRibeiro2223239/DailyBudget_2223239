using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace DailyBudgetWPF.Vistas
{
    public enum TipoMensagem
    {
        Info,
        Sucesso,
        Aviso,
        Erro,
        Questao
    }

    public partial class CaixaMensagem : Window
    {
        public CaixaMensagem()
        {
            InitializeComponent();
            this.MouseLeftButtonDown += CaixaMensagem_MouseLeftButtonDown;
        }

        private void CaixaMensagem_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private void Fechar_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void Cancelar_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void Confirmar_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        public static void Mostrar(string mensagem, string titulo = "Aviso", TipoMensagem tipo = TipoMensagem.Info)
        {
            try
            {
                CaixaMensagem cm = new CaixaMensagem();
                cm.lblMensagem.Text = mensagem;
                cm.lblTitulo.Text = titulo;
                cm.btnCancelar.Visibility = Visibility.Collapsed;
                cm.btnConfirmar.Content = "OK";

                ConfigurarDesign(cm, tipo);

                // Procura a janela ativa atual (pode não ser MainWindow)
                var janelaAtiva = Application.Current?.Windows
                    .OfType<Window>()
                    .FirstOrDefault(w => w.IsActive && w.IsVisible)
                    ?? Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsVisible);

                if (janelaAtiva != null)
                {
                    cm.Owner = janelaAtiva;
                    cm.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                }

                cm.ShowDialog();
            }
            catch
            {
                // Fallback de segurança para não crashar a app
                MessageBox.Show(mensagem, titulo, MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        public static bool Confirmar(string mensagem, string titulo = "Confirmar")
        {
            try
            {
                CaixaMensagem cm = new CaixaMensagem();
                cm.lblMensagem.Text = mensagem;
                cm.lblTitulo.Text = titulo;
                cm.btnCancelar.Visibility = Visibility.Visible;
                cm.btnCancelar.Content = "NÃO";
                cm.btnConfirmar.Content = "SIM";

                ConfigurarDesign(cm, TipoMensagem.Questao);

                // Procura a janela ativa atual
                var janelaAtiva = Application.Current?.Windows
                    .OfType<Window>()
                    .FirstOrDefault(w => w.IsActive && w.IsVisible)
                    ?? Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsVisible);

                if (janelaAtiva != null)
                {
                    cm.Owner = janelaAtiva;
                    cm.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                }

                return cm.ShowDialog() == true;
            }
            catch
            {
                // Fallback de segurança para não crashar a app
                return MessageBox.Show(mensagem, titulo, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
            }
        }

        private static void ConfigurarDesign(CaixaMensagem cm, TipoMensagem tipo)
        {
            Color corBorda = Color.FromRgb(46, 204, 113); // Default Verde
            string icone = "ℹ️";
            Color corIconeBg = Color.FromRgb(46, 204, 113);

            switch (tipo)
            {
                case TipoMensagem.Sucesso:
                    corBorda = Color.FromRgb(46, 204, 113); // Verde #2ECC71
                    corIconeBg = Color.FromRgb(46, 204, 113);
                    icone = "✅";
                    break;
                case TipoMensagem.Info:
                    corBorda = Color.FromRgb(52, 152, 219); // Azul #3498DB
                    corIconeBg = Color.FromRgb(52, 152, 219);
                    icone = "ℹ️";
                    break;
                case TipoMensagem.Aviso:
                    corBorda = Color.FromRgb(243, 156, 18); // Laranja #F39C12
                    corIconeBg = Color.FromRgb(243, 156, 18);
                    icone = "⚠️";
                    break;
                case TipoMensagem.Erro:
                    corBorda = Color.FromRgb(231, 76, 60); // Vermelho #E74C3C
                    corIconeBg = Color.FromRgb(231, 76, 60);
                    icone = "❌";
                    break;
                case TipoMensagem.Questao:
                    corBorda = Color.FromRgb(155, 89, 182); // Roxo #9B59B6
                    corIconeBg = Color.FromRgb(155, 89, 182);
                    icone = "❓";
                    break;
            }

            cm.brdBordaCor.Color = corBorda;
            cm.txtIcone.Text = icone;
            
            // Fazer um background sutil atrás do emoji
            cm.brdIcone.Background = new SolidColorBrush(Color.FromArgb(40, corIconeBg.R, corIconeBg.G, corIconeBg.B));
            cm.btnConfirmar.Background = new SolidColorBrush(corBorda);
        }
    }
}
