using System.Windows;
using DailyBudgetWPF.Dados;
using System;
using System.IO;
using System.Threading.Tasks;

namespace DailyBudgetWPF
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // Captura exceções no thread principal (UI)
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
            
            // Captura exceções em outros threads
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            
            // Captura exceções não observadas em Tasks
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

            base.OnStartup(e);
            ConexaoBD.InicializarBancoDeDados();
            GestorTemas.InicializarTema(); // carrega tema guardado
        }

        private void App_DispatcherUnhandledException(object? sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            RegistarErro("DispatcherUnhandledException", e.Exception);
            e.Handled = true; // Evita crash imediato se possível, mas alerta
            MessageBox.Show($"Ocorreu um erro na aplicação:\n{e.Exception.Message}\n\nDetalhes guardados em crash_log.txt", "Erro Crítico", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void CurrentDomain_UnhandledException(object? sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                RegistarErro("UnhandledException", ex);
            }
        }

        private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            RegistarErro("UnobservedTaskException", e.Exception);
            e.SetObserved();
        }

        private void RegistarErro(string tipo, Exception ex)
        {
            try
            {
                string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash_log.txt");
                string logText = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{tipo}]\n" +
                                 $"Mensagem: {ex.Message}\n" +
                                 $"Source: {ex.Source}\n" +
                                 $"StackTrace:\n{ex.StackTrace}\n";
                if (ex.InnerException != null)
                {
                    logText += $"Inner Exception: {ex.InnerException.Message}\n" +
                               $"Inner StackTrace:\n{ex.InnerException.StackTrace}\n";
                }
                logText += new string('-', 60) + "\n";
                File.AppendAllText(logPath, logText);
            }
            catch
            {
                // Ignora falhas ao escrever log
            }
        }
    }
}
