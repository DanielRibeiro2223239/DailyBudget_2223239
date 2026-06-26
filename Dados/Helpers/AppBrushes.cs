using System.Windows.Media;

namespace DailyBudgetWPF.Helpers
{
    /// <summary>
    /// Classe centralizada de brushes para uso em todo o projeto.
    /// Elimina a duplicação da classe Brushes definida em múltiplos ficheiros.
    /// </summary>
    public static class AppBrushes
    {
        public static readonly Brush Verde = CriarBrush("#2ECC71");
        public static readonly Brush VerdeFill = CriarBrush("#152ECC71");
        public static readonly Brush Vermelho = CriarBrush("#E74C3C");
        public static readonly Brush Azul = CriarBrush("#3498DB");
        public static readonly Brush Laranja = CriarBrush("#F39C12");
        public static readonly Brush Cinzento = CriarBrush("#34495E");

        private static Brush CriarBrush(string hex)
        {
            var brush = (Brush)new BrushConverter().ConvertFromString(hex)!;
            brush.Freeze();
            return brush;
        }

        /// <summary>Retorna Verde se ativo, Cinzento caso contrário — para botões de tipo de gráfico.</summary>
        public static Brush BotaoAtivo(bool ativo) => ativo ? Verde : Cinzento;

        /// <summary>Converte uma string hex de cor num Brush.</summary>
        public static Brush DeCor(string hex)
        {
            var brush = (Brush)new BrushConverter().ConvertFromString(hex)!;
            brush.Freeze();
            return brush;
        }
    }
}
