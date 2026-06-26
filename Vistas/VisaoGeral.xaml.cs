using DailyBudgetWPF.Dados.Repositorios;
using DailyBudgetWPF.Dados;
using DailyBudgetWPF.Helpers;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using LiveCharts;
using LiveCharts.Wpf;
using DailyBudgetWPF.Modelos;

namespace DailyBudgetWPF.Vistas
{
    public partial class VisaoGeral : UserControl
    {
        private string _tipoAtual = "Line";

        public VisaoGeral()
        {
            InitializeComponent();
            this.Loaded += (s, e) => CarregarDados();
        }

        private void CarregarDados()
        {
            if (Sessao.UtilizadorAtual == null) return;

            UiHelper.ExecutarComTratamento(() =>
            {
                int userId = Sessao.UtilizadorAtual.Id;

                decimal tr = RepositorioTransacoes.ObterTotalReceitas(userId);
                decimal td = RepositorioTransacoes.ObterTotalDespesas(userId);
                decimal saldo = tr - td;

                txtSaldoGrande.Text = $"{saldo:N2} €";
                txtReceitasDashboard.Text = $"{tr:N2} €";
                txtDespesasDashboard.Text = $"{td:N2} €";

                // Indicador de tendência (comparar último mês vs penúltimo)
                AtualizarTendencias(userId);

                AtualizarGrafico(userId);
                lstAtividade.ItemsSource = RepositorioTransacoes.ObterRecentes(userId);
                AtualizarAlertas(userId);

            }, "Erro ao carregar dados.");
        }

        private void AtualizarTendencias(int userId)
        {
            try
            {
                var historico = RepositorioTransacoes.ObterRelatorioConsolidado(userId, "Mensal");
                if (historico == null || historico.Count < 2) return;

                var mesAtual    = historico[^1];
                var mesAnterior = historico[^2];

                decimal saldoAtual    = mesAtual.Receitas    - mesAtual.Despesas;
                decimal saldoAnterior = mesAnterior.Receitas - mesAnterior.Despesas;

                // Tendência saldo
                if (saldoAnterior != 0)
                {
                    decimal pct = (saldoAtual - saldoAnterior) / Math.Abs(saldoAnterior) * 100;
                    if (txtTendenciaSaldo != null)
                    {
                        txtTendenciaSaldo.Text = pct >= 0
                            ? $"▲ +{pct:0}% vs mês anterior"
                            : $"▼ {pct:0}% vs mês anterior";
                        txtTendenciaSaldo.Foreground = pct >= 0 ? AppBrushes.Verde : AppBrushes.Vermelho;
                    }
                }

                // Tendência receitas
                if (mesAnterior.Receitas != 0 && txtTendenciaReceitas != null)
                {
                    decimal pct = (mesAtual.Receitas - mesAnterior.Receitas) / mesAnterior.Receitas * 100;
                    txtTendenciaReceitas.Text = pct >= 0
                        ? $"▲ +{pct:0}% vs mês anterior"
                        : $"▼ {pct:0}% vs mês anterior";
                    txtTendenciaReceitas.Foreground = pct >= 0 ? AppBrushes.Verde : AppBrushes.Vermelho;
                }

                // Tendência despesas (inverso: menos despesa = bom)
                if (mesAnterior.Despesas != 0 && txtTendenciaDespesas != null)
                {
                    decimal pct = (mesAtual.Despesas - mesAnterior.Despesas) / mesAnterior.Despesas * 100;
                    txtTendenciaDespesas.Text = pct <= 0
                        ? $"▼ {Math.Abs(pct):0}% vs mês anterior"
                        : $"▲ +{pct:0}% vs mês anterior";
                    txtTendenciaDespesas.Foreground = pct <= 0 ? AppBrushes.Verde : AppBrushes.Vermelho;
                }
            }
            catch
            {
                // Ignorar se não houver dados suficientes para tendência
            }
        }

        private void AtualizarGrafico(int userId)
        {
            var historico = RepositorioTransacoes.ObterRelatorioConsolidado(userId, "Mensal");
            if (historico == null || historico.Count == 0)
                historico = new System.Collections.Generic.List<(string, decimal, decimal)>
                    { (DateTime.Now.ToString("MM/yyyy"), 0, 0) };

            // Calcular saldo ACUMULADO (running total) para que o último ponto = saldo atual
            var acumulados = new System.Collections.Generic.List<double>();
            double acumulado = 0;
            foreach (var h in historico)
            {
                acumulado += (double)(h.Receitas - h.Despesas);
                acumulados.Add(acumulado);
            }

            var valores = new ChartValues<double>(acumulados);
            var labels = historico.Select(h => h.Periodo).ToList();

            if (valores.Count == 1) { valores.Insert(0, 0); labels.Insert(0, ""); }

            chartPatrimonio.Series = new SeriesCollection();
            if (_tipoAtual == "Line")
            {
                chartPatrimonio.Series.Add(new LineSeries
                {
                    Title = "Saldo",
                    Values = valores,
                    Stroke = AppBrushes.Verde,
                    Fill = AppBrushes.VerdeFill,
                    PointGeometrySize = 8
                });
            }
            else
            {
                chartPatrimonio.Series.Add(new ColumnSeries
                {
                    Title = "Saldo",
                    Values = valores,
                    Fill = AppBrushes.Verde,
                    MaxColumnWidth = 40
                });
            }

            axisX.Labels = labels.ToArray();
            var brushTextoSec = TryFindResource("CorTextoSec") as Brush ?? Brushes.Gray;
            axisX.Foreground = brushTextoSec;
            if (chartPatrimonio.AxisY.Count > 0)
                chartPatrimonio.AxisY[0].Foreground = brushTextoSec;

            chartPatrimonio.AxisY[0].LabelFormatter = v => v.ToString("N0") + " €";
            chartPatrimonio.Update(true, true);
        }

        private void AtualizarAlertas(int userId)
        {
            try
            {
                var alertas = RepositorioOrcamentos.ObterAlertas(userId);
                icAlertas.ItemsSource = alertas;
                bordAlertas.Visibility = alertas.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            catch
            {
                bordAlertas.Visibility = Visibility.Collapsed;
            }
        }

        private void btnChartType_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null)
            {
                _tipoAtual = btn.Tag.ToString()!;
                btnLine.Background = AppBrushes.BotaoAtivo(_tipoAtual == "Line");
                btnBar.Background  = AppBrushes.BotaoAtivo(_tipoAtual == "Bar");
                if (Sessao.UtilizadorAtual != null) AtualizarGrafico(Sessao.UtilizadorAtual.Id);
            }
        }

        private void btnAddReceita_Click(object sender, RoutedEventArgs e)
        {
            if (Window.GetWindow(this) is ShellWindow shell) shell.Navegar(new Receitas());
        }

        private void btnAddDespesa_Click(object sender, RoutedEventArgs e)
        {
            if (Window.GetWindow(this) is ShellWindow shell) shell.Navegar(new Despesas());
        }
    }
}
