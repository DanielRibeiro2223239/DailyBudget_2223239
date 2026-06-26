using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DailyBudgetWPF.Dados;
using DailyBudgetWPF.Modelos;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Oauth2.v2;
using Google.Apis.Services;
using Microsoft.Data.SqlClient;

namespace DailyBudgetWPF.Vistas
{
    public partial class MainWindow : Window
    {
        public MainWindow() => InitializeComponent();

        private void Hero_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
            => DragMove();

        private void Minimize_Click(object sender, RoutedEventArgs e)
            => WindowState = WindowState.Minimized;

        private void Close_Click(object sender, RoutedEventArgs e)
            => Application.Current.Shutdown();

        private void Register_Click(object sender, RoutedEventArgs e)
        {
            new RegisterWindow().Show();
            Close();
        }

        // ─── Focus glow para inputs ────────────────────────────────────────────
        private void Input_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement el && el.Tag is string name && FindName(name) is Border b)
            {
                b.BorderBrush = new SolidColorBrush(Color.FromArgb(0x60, 0x2E, 0xCC, 0x71));
                b.Background = new SolidColorBrush(Color.FromArgb(0x14, 0xFF, 0xFF, 0xFF));
            }
        }

        private void Input_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement el && el.Tag is string name && FindName(name) is Border b)
            {
                b.BorderBrush = new SolidColorBrush(Color.FromArgb(0x14, 0xFF, 0xFF, 0xFF));
                b.Background = new SolidColorBrush(Color.FromArgb(0x0C, 0xFF, 0xFF, 0xFF));
            }
        }



        // ─── Login normal ──────────────────────────────────────────────────────
        private void Login_Click(object sender, RoutedEventArgs e)
        {
            string email = txtEmail.Text.Trim();
            string password = txtPassword.Password;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            { MostrarErro("Por favor preenche todos os campos."); return; }

            try
            {
                string hash = ConexaoBD.GerarHash(password);
                using var conn = ConexaoBD.ObterConexao();
                conn.Open();

                const string sql = "SELECT Id, Nome, Email, Username FROM Utilizadores WHERE Email = @Email AND Senha = @Senha";
                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@Email", email);
                cmd.Parameters.AddWithValue("@Senha", hash);

                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    Sessao.UtilizadorAtual = new Utilizador
                    {
                        Id = reader.GetInt32(0), Nome = reader.GetString(1),
                        Email = reader.GetString(2), Username = reader.GetString(3)
                    };
                    AbrirShell();
                }
                else
                    MostrarErro("Email ou palavra-passe incorretos.");
            }
            catch (Exception ex) { MostrarErro("Erro na ligação à base de dados: " + ex.Message); }
        }

        // Autenticação via conta Google (OAuth2)
        private async void btnGoogle_Click(object sender, RoutedEventArgs e)
        {
            btnGoogle.IsEnabled = false;
            btnGoogle.Content = "A autenticar...";
            try
            {
                string secretsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources",
                    "client_secret_1001168147172-9os2q4oss9s2sssrh1palpnie4k72qau.apps.googleusercontent.com.json");
                if (!File.Exists(secretsPath))
                    secretsPath = "Resources/client_secret_1001168147172-9os2q4oss9s2sssrh1palpnie4k72qau.apps.googleusercontent.com.json";
                if (!File.Exists(secretsPath))
                { MostrarErro("Ficheiro de credenciais Google não encontrado."); return; }

                string[] scopes = { Oauth2Service.Scope.UserinfoEmail, Oauth2Service.Scope.UserinfoProfile };
                string tokenFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DailyBudget_GoogleAuth");
                if (Directory.Exists(tokenFolder)) try { Directory.Delete(tokenFolder, true); } catch { }

                using var stream = new FileStream(secretsPath, FileMode.Open, FileAccess.Read);
                var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.FromStream(stream).Secrets, scopes, "user",
                    CancellationToken.None, new Google.Apis.Util.Store.FileDataStore(tokenFolder, true));

                var oauthService = new Oauth2Service(new BaseClientService.Initializer
                    { HttpClientInitializer = credential, ApplicationName = "DailyBudget" });
                var userInfo = await oauthService.Userinfo.Get().ExecuteAsync();
                string nomeGoogle = userInfo.Name ?? userInfo.Email ?? "Utilizador";
                string emailGoogle = userInfo.Email ?? string.Empty;
                if (string.IsNullOrEmpty(emailGoogle))
                { MostrarErro("Não foi possível obter o email da conta Google."); return; }

                using var conn = ConexaoBD.ObterConexao();
                conn.Open();
                using (var cmdSel = new SqlCommand("SELECT Id, Nome, Email, Username FROM Utilizadores WHERE Email = @Email", conn))
                {
                    cmdSel.Parameters.AddWithValue("@Email", emailGoogle);
                    using var reader = cmdSel.ExecuteReader();
                    if (reader.Read())
                    {
                        Sessao.UtilizadorAtual = new Utilizador
                        { Id = reader.GetInt32(0), Nome = reader.GetString(1), Email = reader.GetString(2), Username = reader.GetString(3) };
                        AbrirShell(); return;
                    }
                }

                string usernameGoogle = emailGoogle.Split('@')[0].ToLower();
                using (var cmdUnico = new SqlCommand("SELECT COUNT(*) FROM Utilizadores WHERE Username = @Username", conn))
                {
                    cmdUnico.Parameters.AddWithValue("@Username", usernameGoogle);
                    if ((int)cmdUnico.ExecuteScalar() > 0) usernameGoogle += new Random().Next(100, 999);
                }

                string senhaAleatoria = ConexaoBD.GerarHash(Guid.NewGuid().ToString("N"));
                const string sqlIns = "INSERT INTO Utilizadores (Nome, Username, Email, Senha) OUTPUT INSERTED.Id VALUES (@Nome, @Username, @Email, @Senha)";
                using var cmdIns = new SqlCommand(sqlIns, conn);
                cmdIns.Parameters.AddWithValue("@Nome", nomeGoogle);
                cmdIns.Parameters.AddWithValue("@Username", usernameGoogle);
                cmdIns.Parameters.AddWithValue("@Email", emailGoogle);
                cmdIns.Parameters.AddWithValue("@Senha", senhaAleatoria);
                int novoId = (int)cmdIns.ExecuteScalar();
                Sessao.UtilizadorAtual = new Utilizador { Id = novoId, Nome = nomeGoogle, Username = usernameGoogle, Email = emailGoogle };
                // Criar categorias predefinidas para a nova conta Google
                Dados.Repositorios.RepositorioCategorias.CriarCategoriasPredefinidas(novoId);
                AbrirShell();
            }
            catch (Exception ex) { MostrarErro("Erro na autenticação Google:\n" + ex.Message); }
            finally { btnGoogle.IsEnabled = true; btnGoogle.Content = "Continuar com Google"; }
        }

        private void MostrarErro(string msg) { txtErro.Text = msg; pnlErro.Visibility = Visibility.Visible; }
        private void AbrirShell() { new ShellWindow().Show(); Close(); }

        // Mostrar/esconder palavra-passe
        private bool _isSyncingPasswords = false;

        private void btnShowPassword_Click(object sender, RoutedEventArgs e)
        {
            if (btnShowPassword.IsChecked == true)
            {
                txtPasswordUnmasked.Text = txtPassword.Password;
                txtPassword.Visibility = Visibility.Collapsed;
                txtPasswordUnmasked.Visibility = Visibility.Visible;
                txtPasswordUnmasked.Focus();
            }
            else
            {
                txtPassword.Password = txtPasswordUnmasked.Text;
                txtPasswordUnmasked.Visibility = Visibility.Collapsed;
                txtPassword.Visibility = Visibility.Visible;
                txtPassword.Focus();
            }
        }

        private void txtPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_isSyncingPasswords) return;
            _isSyncingPasswords = true;
            txtPasswordUnmasked.Text = txtPassword.Password;
            _isSyncingPasswords = false;
        }

        private void txtPasswordUnmasked_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isSyncingPasswords) return;
            _isSyncingPasswords = true;
            txtPassword.Password = txtPasswordUnmasked.Text;
            _isSyncingPasswords = false;
        }
    }
}
