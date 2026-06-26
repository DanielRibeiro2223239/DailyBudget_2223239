using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Media;
using DailyBudgetWPF.Dados;
using DailyBudgetWPF.Modelos;
using Microsoft.Data.SqlClient;

namespace DailyBudgetWPF.Vistas
{
    public partial class RegisterWindow : Window
    {
        public RegisterWindow() => InitializeComponent();

        // Impede que o utilizador escreva espaços no campo Username
        private void txtUsername_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space) e.Handled = true;
        }

        private void txtUsername_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txtUsername.Text.Contains(" "))
            {
                int caretIndex = txtUsername.CaretIndex;
                txtUsername.Text = txtUsername.Text.Replace(" ", "");
                txtUsername.CaretIndex = Math.Min(caretIndex, txtUsername.Text.Length);
            }
        }

        // Permite arrastar a janela
        private void Hero_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) DragMove();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
            => WindowState = WindowState.Minimized;

        private void Close_Click(object sender, RoutedEventArgs e)
            => Application.Current.Shutdown();

        // Efeito visual ao focar nos campos de texto
        private void Input_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement el && el.Tag is string name && FindName(name) is Border b)
            {
                b.BorderBrush = new SolidColorBrush(Color.FromArgb(0x70, 0x6C, 0x5C, 0xE7));
                b.Background  = new SolidColorBrush(Color.FromArgb(0x16, 0xFF, 0xFF, 0xFF));
            }
        }

        private void Input_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement el && el.Tag is string name && FindName(name) is Border b)
            {
                b.BorderBrush = new SolidColorBrush(Color.FromArgb(0x14, 0xFF, 0xFF, 0xFF));
                b.Background  = new SolidColorBrush(Color.FromArgb(0x0C, 0xFF, 0xFF, 0xFF));
            }
        }

        // Registo de nova conta
        private void Register_Click(object sender, RoutedEventArgs e)
        {
            EsconderFeedback();

            string nome     = txtNome.Text.Trim();
            string username = txtUsername.Text.Trim().ToLower();
            string email    = txtEmail.Text.Trim();
            string senha    = txtPassword.Password;
            string confirmar = txtConfirmPassword.Password;

            // ── Validações ─────────────────────────────────────────────────────
            if (string.IsNullOrEmpty(nome) || string.IsNullOrEmpty(username) ||
                string.IsNullOrEmpty(email) || string.IsNullOrEmpty(senha) || string.IsNullOrEmpty(confirmar))
            { MostrarErro("Por favor preenche todos os campos."); return; }

            if (nome.Length < 2)
            { MostrarErro("O nome deve ter pelo menos 2 caracteres."); return; }

            if (username.Length < 3)
            { MostrarErro("O nome de utilizador deve ter pelo menos 3 caracteres."); return; }

            if (!email.Contains("@") || !email.Contains("."))
            { MostrarErro("Introduz um email válido."); return; }

            if (senha.Length < 6)
            { MostrarErro("A palavra-passe deve ter pelo menos 6 caracteres."); return; }

            if (senha != confirmar)
            { MostrarErro("As palavras-passe não coincidem."); return; }

            btnRegistar.IsEnabled = false;
            Cursor = Cursors.Wait;

            try
            {
                using var conn = ConexaoBD.ObterConexao();
                conn.Open();

                // 1. Verificar email duplicado
                using (var cmd = new SqlCommand("SELECT Senha FROM Utilizadores WHERE Email = @Email", conn))
                {
                    cmd.Parameters.AddWithValue("@Email", email);
                    var resultado = cmd.ExecuteScalar();
                    if (resultado != null)
                    {
                        string senhaExistente = resultado.ToString() ?? "";
                        bool isGoogle = senhaExistente.Length == 64 &&
                            senhaExistente.All(c => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f'));
                        MostrarErro(isGoogle
                            ? "Este email já foi usado para entrar com Google. Usa o botão Google no ecrã de login."
                            : "Este email já está registado. Tenta iniciar sessão.");
                        return;
                    }
                }

                // 2. Verificar username duplicado
                using (var cmd = new SqlCommand("SELECT COUNT(*) FROM Utilizadores WHERE Username = @Username", conn))
                {
                    cmd.Parameters.AddWithValue("@Username", username);
                    if ((int)cmd.ExecuteScalar() > 0)
                    { MostrarErro("Este nome de utilizador já está a ser usado."); return; }
                }

                // 3. Inserir utilizador
                string hash = ConexaoBD.GerarHash(senha);
                const string sqlInsert =
                    "INSERT INTO Utilizadores (Nome, Username, Email, Senha) OUTPUT INSERTED.Id VALUES (@Nome, @Username, @Email, @Senha)";
                using var cmdInsert = new SqlCommand(sqlInsert, conn);
                cmdInsert.Parameters.AddWithValue("@Nome",     nome);
                cmdInsert.Parameters.AddWithValue("@Username", username);
                cmdInsert.Parameters.AddWithValue("@Email",    email);
                cmdInsert.Parameters.AddWithValue("@Senha",    hash);
                int novoId = (int)cmdInsert.ExecuteScalar();

                // Login automático
                Sessao.UtilizadorAtual = new Utilizador
                {
                    Id       = novoId,
                    Nome     = nome,
                    Username = username,
                    Email    = email
                };

                // Criar categorias predefinidas para o novo utilizador
                Dados.Repositorios.RepositorioCategorias.CriarCategoriasPredefinidas(novoId);

                MostrarSucesso("Conta criada com sucesso! A entrar na aplicação...");

                var timer = new System.Windows.Threading.DispatcherTimer
                    { Interval = TimeSpan.FromSeconds(1.2) };
                timer.Tick += (s, args) =>
                {
                    timer.Stop();
                    new ShellWindow().Show();
                    Close();
                };
                timer.Start();
            }
            catch (Exception ex)
            {
                MostrarErro("Erro ao criar conta: " + ex.Message);
            }
            finally
            {
                btnRegistar.IsEnabled = true;
                Cursor = Cursors.Arrow;
            }
        }

        // Voltar ao ecrã de login
        private void Back_Click(object sender, RoutedEventArgs e)
        {
            new MainWindow().Show();
            Close();
        }

        // Métodos auxiliares de feedback visual
        private void MostrarErro(string msg)
        {
            pnlSucesso.Visibility = Visibility.Collapsed;
            txtErro.Text          = msg;
            pnlErro.Visibility    = Visibility.Visible;
        }

        private void MostrarSucesso(string msg)
        {
            pnlErro.Visibility     = Visibility.Collapsed;
            txtSucesso.Text        = msg;
            pnlSucesso.Visibility  = Visibility.Visible;
        }

        private void EsconderFeedback()
        {
            pnlErro.Visibility    = Visibility.Collapsed;
            pnlSucesso.Visibility = Visibility.Collapsed;
        }

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

        private void btnShowConfirmPassword_Click(object sender, RoutedEventArgs e)
        {
            if (btnShowConfirmPassword.IsChecked == true)
            {
                txtConfirmPasswordUnmasked.Text = txtConfirmPassword.Password;
                txtConfirmPassword.Visibility = Visibility.Collapsed;
                txtConfirmPasswordUnmasked.Visibility = Visibility.Visible;
                txtConfirmPasswordUnmasked.Focus();
            }
            else
            {
                txtConfirmPassword.Password = txtConfirmPasswordUnmasked.Text;
                txtConfirmPasswordUnmasked.Visibility = Visibility.Collapsed;
                txtConfirmPassword.Visibility = Visibility.Visible;
                txtConfirmPassword.Focus();
            }
        }

        private void txtConfirmPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_isSyncingPasswords) return;
            _isSyncingPasswords = true;
            txtConfirmPasswordUnmasked.Text = txtConfirmPassword.Password;
            _isSyncingPasswords = false;
        }

        private void txtConfirmPasswordUnmasked_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isSyncingPasswords) return;
            _isSyncingPasswords = true;
            txtConfirmPassword.Password = txtConfirmPasswordUnmasked.Text;
            _isSyncingPasswords = false;
        }
    }
}
