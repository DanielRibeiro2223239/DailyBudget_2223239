using System;
using System.Windows.Media;
using DailyBudgetWPF.Helpers;

namespace DailyBudgetWPF.Modelos
{
    public class Utilizador
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class Categoria
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Emoji { get; set; } = "📁";
        public string CorHex { get; set; } = "#27AE60";
        public string NomeExibicao => $"{Emoji} {Nome}";
        public Brush CorBrush
        {
            get
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(CorHex))
                        return Brushes.Transparent;
                    return AppBrushes.DeCor(CorHex);
                }
                catch
                {
                    return Brushes.Transparent;
                }
            }
        }
    }

    public class Transacao
    {
        public int Id { get; set; }
        public string Descricao { get; set; } = string.Empty;
        public decimal Valor { get; set; }
        public DateTime Data { get; set; }
        public string Tipo { get; set; } = "Despesa";
        public string Categoria { get; set; } = "Geral";
        public string Icone => Tipo == "Receita" ? "💰" : "💸";
        public Brush CorValor => Tipo == "Receita" ? AppBrushes.Verde : AppBrushes.Vermelho;
    }

    public class ItemDesejado
    {
        public int Id { get; set; }
        public string Item { get; set; } = string.Empty;
        public decimal ValorEstimado { get; set; }
        public int Prioridade { get; set; }
        public bool Adquirido { get; set; }
        /// <summary>Saldo atual do utilizador — injetado ao carregar a lista.</summary>
        public decimal SaldoAtual { get; set; }

        public string StatusIcone => Adquirido ? "✅" : "⏳";
        public string TextoPrioridade => Prioridade switch { 3 => "ALTA", 2 => "MÉDIA", _ => "BAIXA" };
        public Brush CorPrioridade => Prioridade switch { 3 => AppBrushes.Vermelho, 2 => AppBrushes.Laranja, _ => AppBrushes.Azul };

        /// <summary>Percentagem do valor estimado coberta pelo saldo atual (0-100).</summary>
        public double PercentagemPoupanca =>
            ValorEstimado <= 0 ? 100 : Math.Min(100, (double)(SaldoAtual / ValorEstimado * 100));
        public string TextoProgresso =>
            ValorEstimado <= 0 ? "Gratuito" : $"{SaldoAtual:N0} € / {ValorEstimado:N0} €";
        public string TextoPercentagem =>
            Adquirido ? "✅ Adquirido" : $"{PercentagemPoupanca:0}%";
        /// <summary>Largura da barra de progresso em pixels (max ~219px para caber no card de 255px).</summary>
        public double LarguraBarra => Math.Max(0, PercentagemPoupanca / 100.0 * 219);
        public Brush CorProgresso => PercentagemPoupanca >= 100 ? AppBrushes.Verde
            : PercentagemPoupanca >= 50 ? AppBrushes.Laranja
            : AppBrushes.Vermelho;
    }

    public class OrcamentoCategoria
    {
        public int Id { get; set; }
        public int IdCategoria { get; set; }
        public string NomeCategoria { get; set; } = string.Empty;
        public string EmojiCategoria { get; set; } = "📁";
        public decimal LimiteMensal { get; set; }
        public decimal GastoMesAtual { get; set; }
        public double Percentagem =>
            LimiteMensal <= 0 ? 0 : Math.Max(0, Math.Min(100, (double)(GastoMesAtual / LimiteMensal * 100)));
        public bool Ultrapassado => GastoMesAtual > LimiteMensal;
        public string TextoEstado => Ultrapassado ? "⚠️ Limite ultrapassado!" : $"{Percentagem:0}% usado";
        public Brush CorEstado => Ultrapassado ? AppBrushes.Vermelho
            : Percentagem >= 80 ? AppBrushes.Laranja
            : AppBrushes.Verde;
    }
}
