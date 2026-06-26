using DailyBudgetWPF.Dados.Repositorios;
using DailyBudgetWPF.Dados;
using System.Windows;
using System.Windows.Controls;

namespace DailyBudgetWPF.Vistas
{
    public partial class ShellWindow : Window
    {
        public ShellWindow()
        {
            InitializeComponent();
            Navegar(new VisaoGeral());
            var utilizador = Sessao.UtilizadorAtual;
            if (utilizador != null)
            {
                txtUserName.Text = utilizador.Nome?.ToUpper() ?? "UTILIZADOR";
                txtUserUsername.Text = $"@{utilizador.Username ?? "utilizador"}";

                // Mostrar iniciais no avatar (ex: "Daniel Ribeiro" → "DR")
                string nome = utilizador.Nome ?? "";
                var partes = nome.Trim().Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
                if (partes.Length >= 2)
                    txtIniciais.Text = $"{partes[0][0]}{partes[^1][0]}".ToUpper();
                else if (partes.Length == 1 && partes[0].Length > 0)
                    txtIniciais.Text = partes[0][0].ToString().ToUpper();
                else
                    txtIniciais.Text = "?";
            }
        }

        public void Navegar(UserControl novaVista)
        {
            ActiveContent.Content = novaVista;
            if (TryFindResource("FadeInAnimation") is System.Windows.Media.Animation.Storyboard sb)
            {
                sb.Begin(ActiveContent);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();
        private void Minimize_Click(object sender, RoutedEventArgs e) => this.WindowState = WindowState.Minimized;

        private void NavDashboard_Click(object sender, RoutedEventArgs e) => Navegar(new VisaoGeral());
        private void NavReceitas_Click(object sender, RoutedEventArgs e) => Navegar(new Receitas());
        private void NavDespesas_Click(object sender, RoutedEventArgs e) => Navegar(new Despesas());
        private void NavCategorias_Click(object sender, RoutedEventArgs e) => Navegar(new Categorias());
        private void NavRelatorios_Click(object sender, RoutedEventArgs e) => Navegar(new Relatorios());
        private void NavWishlist_Click(object sender, RoutedEventArgs e) => Navegar(new ListaDesejos());

        private void ToggleTheme_Click(object sender, RoutedEventArgs e)
        {
            GestorTemas.AlternarTema();
            if (ActiveContent.Content != null)
            {
                var tipo = ActiveContent.Content.GetType();
                var novaInstancia = (UserControl)System.Activator.CreateInstance(tipo)!;
                Navegar(novaInstancia);
            }
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            Sessao.UtilizadorAtual = null!;
            MainWindow login = new MainWindow();
            login.Show();
            this.Close();
        }
    }
}
