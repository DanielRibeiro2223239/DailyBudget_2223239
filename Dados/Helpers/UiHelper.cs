using System;
using System.Diagnostics;
using System.Windows;

namespace DailyBudgetWPF.Helpers
{
    /// <summary>
    /// Classe utilitária para operações de UI comuns,
    /// eliminando código duplicado de tratamento de erros em todas as views.
    /// </summary>
    public static class UiHelper
    {
        /// <summary>
        /// Executa uma ação com tratamento de erro centralizado.
        /// Mostra um MessageBox em caso de exceção.
        /// </summary>
        public static void ExecutarComTratamento(Action acao, string mensagemErro = "Erro ao carregar dados.")
        {
            try
            {
                acao();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[UiHelper] {ex.Message}");
                DailyBudgetWPF.Vistas.CaixaMensagem.Mostrar(
                    mensagemErro + "\nVerifique a ligação à base de dados.",
                    "Erro",
                    DailyBudgetWPF.Vistas.TipoMensagem.Erro);
            }
        }

        /// <summary>
        /// Tenta converter uma string para decimal de forma flexível,
        /// suportando ponto ou vírgula como separador decimal.
        /// </summary>
        public static bool TentarConverterDecimal(string texto, out decimal valor)
        {
            valor = 0;
            if (string.IsNullOrWhiteSpace(texto)) return false;

            string limpo = texto.Trim();

            // Se contiver exatamente um ponto e nenhuma vírgula (ex: 200.00 ou 1.5),
            // assume que o ponto é o separador decimal e substitui por vírgula para a cultura pt-PT.
            int pontos = 0;
            bool temVirgula = false;
            foreach (char c in limpo)
            {
                if (c == '.') pontos++;
                else if (c == ',') temVirgula = true;
            }

            if (pontos == 1 && !temVirgula)
            {
                limpo = limpo.Replace('.', ',');
            }

            // Remove símbolos monetários ou espaços extras se houver
            limpo = limpo.Replace("€", "").Trim();

            return decimal.TryParse(limpo, System.Globalization.NumberStyles.Any, new System.Globalization.CultureInfo("pt-PT"), out valor);
        }

        /// <summary>
        /// Formata o texto de uma TextBox para o padrão decimal pt-PT (ex: 200,00 ou 2.000,00) ao perder o foco.
        /// </summary>
        public static void FormatarTextBoxDecimal(System.Windows.Controls.TextBox tb)
        {
            if (tb == null) return;
            if (TentarConverterDecimal(tb.Text, out decimal valor))
            {
                tb.Text = valor.ToString("N2", new System.Globalization.CultureInfo("pt-PT"));
            }
        }
    }
}
