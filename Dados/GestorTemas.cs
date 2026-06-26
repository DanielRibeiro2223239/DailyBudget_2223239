using System;
using System.IO;
using System.Windows;

namespace DailyBudgetWPF.Dados
{
    public static class GestorTemas
    {
        private static bool _isEscuro = true;

        private static readonly string PastaSettings = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DailyBudget");

        private static string FicheiroSettings => Path.Combine(PastaSettings, "theme.txt");

        /// <summary>
        /// Carrega o tema guardado anteriormente. Deve ser chamado no arranque da app.
        /// </summary>
        public static void InicializarTema()
        {
            _isEscuro = true; // padrão: escuro
            try
            {
                if (File.Exists(FicheiroSettings))
                    _isEscuro = File.ReadAllText(FicheiroSettings).Trim() == "Escuro";
            }
            catch { /* ignora erros de leitura */ }

            AplicarTemaInterno();
        }

        /// <summary>
        /// Alterna entre tema escuro e claro, guardando a preferência.
        /// </summary>
        public static void AlternarTema()
        {
            _isEscuro = !_isEscuro;
            GuardarPreferencia();
            AplicarTemaInterno();
        }

        public static bool IsEscuro => _isEscuro;

        private static void AplicarTemaInterno()
        {
            string novoTema = _isEscuro ? "Temas/TemaEscuro.xaml" : "Temas/TemaClaro.xaml";
            var novoDic = new ResourceDictionary { Source = new Uri(novoTema, UriKind.Relative) };

            for (int i = 0; i < Application.Current.Resources.MergedDictionaries.Count; i++)
            {
                var dict = Application.Current.Resources.MergedDictionaries[i];
                if (dict.Source != null &&
                    (dict.Source.OriginalString.Contains("TemaEscuro") ||
                     dict.Source.OriginalString.Contains("TemaClaro")))
                {
                    Application.Current.Resources.MergedDictionaries[i] = novoDic;
                    return;
                }
            }
            Application.Current.Resources.MergedDictionaries.Add(novoDic);
        }

        private static void GuardarPreferencia()
        {
            try
            {
                Directory.CreateDirectory(PastaSettings);
                File.WriteAllText(FicheiroSettings, _isEscuro ? "Escuro" : "Claro");
            }
            catch { /* ignora erros de escrita */ }
        }
    }
}
