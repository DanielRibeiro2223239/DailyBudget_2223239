using DailyBudgetWPF.Dados.Repositorios;
using DailyBudgetWPF.Dados;
using DailyBudgetWPF.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DailyBudgetWPF.Modelos;
using System.IO;
using Microsoft.Win32;
using System.Text;
using LiveCharts;
using LiveCharts.Wpf;

namespace DailyBudgetWPF.Vistas
{


    public partial class Relatorios : UserControl
    {
        private int _tipoGrafico = 0;

        public Relatorios()
        {
            InitializeComponent();
            this.Loaded += (s, e) => CarregarDados();
        }

        private void CarregarDados()
        {
            if (Sessao.UtilizadorAtual == null) return;

            UiHelper.ExecutarComTratamento(() =>
            {
                var dados = RepositorioTransacoes.ObterRelatorioConsolidado(Sessao.UtilizadorAtual.Id, "Mensal");
                var gastosPorCategoria = RepositorioTransacoes.ObterGastosPorCategoria(Sessao.UtilizadorAtual.Id);

                bool temDados = dados != null && dados.Count > 0;

                if (!temDados)
                {
                    txtSemDados.Visibility = Visibility.Visible;
                    chartRelatorio.Visibility = Visibility.Collapsed;
                    chartPie.Visibility = Visibility.Collapsed;
                }
                else
                {
                    txtSemDados.Visibility = Visibility.Collapsed;
                    AtualizarGrafico(dados!, gastosPorCategoria);
                    AtualizarIndicadores(dados!, gastosPorCategoria);
                }

            }, "Erro ao carregar os dados do relatório.");
        }

        // ─── Gráfico ─────────────────────────────────────────────────────────────
        private void AtualizarGrafico(
            List<(string Periodo, decimal Receitas, decimal Despesas)> dados,
            List<(string Categoria, decimal Total)> gastosPorCategoria)
        {
            chartRelatorio.Visibility = _tipoGrafico < 2 ? Visibility.Visible : Visibility.Collapsed;
            chartPie.Visibility = _tipoGrafico == 2 ? Visibility.Visible : Visibility.Collapsed;

            // Obter cor de texto do tema atual para os eixos
            var corTextoSec = TryFindResource("CorTextoSec") as Brush ?? Brushes.Gray;

            if (_tipoGrafico < 2)
            {
                chartRelatorio.Series = new SeriesCollection();
                var valsR = new ChartValues<double>(dados.Select(d => (double)d.Receitas));
                var valsD = new ChartValues<double>(dados.Select(d => (double)d.Despesas));

                if (_tipoGrafico == 0)
                {
                    chartRelatorio.Series.Add(new ColumnSeries
                    {
                        Title = "Receitas",
                        Values = valsR,
                        Fill = AppBrushes.Verde,
                        MaxColumnWidth = 35
                    });
                    chartRelatorio.Series.Add(new ColumnSeries
                    {
                        Title = "Despesas",
                        Values = valsD,
                        Fill = AppBrushes.Vermelho,
                        MaxColumnWidth = 35
                    });
                }
                else
                {
                    chartRelatorio.Series.Add(new LineSeries
                    {
                        Title = "Receitas",
                        Values = valsR,
                        Stroke = AppBrushes.Verde,
                        Fill = AppBrushes.VerdeFill,
                        PointGeometrySize = 8
                    });
                    chartRelatorio.Series.Add(new LineSeries
                    {
                        Title = "Despesas",
                        Values = valsD,
                        Stroke = AppBrushes.Vermelho,
                        Fill = System.Windows.Media.Brushes.Transparent,
                        PointGeometrySize = 8
                    });
                }

                axisX.Labels = dados.Select(d => d.Periodo).ToArray();
                axisX.Foreground = corTextoSec;
                chartRelatorio.AxisY[0].Foreground = corTextoSec;
                chartRelatorio.AxisY[0].LabelFormatter = v => v.ToString("N0") + " €";
                chartRelatorio.Update(true, true);
            }
            else
            {
                chartPie.Series = new SeriesCollection();
                foreach (var g in gastosPorCategoria)
                {
                    chartPie.Series.Add(new PieSeries
                    {
                        Title = g.Categoria,
                        Values = new ChartValues<double> { (double)g.Total },
                        DataLabels = true
                    });
                }
                
                // Resolver o posicionamento no canto superior esquerdo forçando um pass de layout
                Dispatcher.BeginInvoke(new Action(() => {
                    chartPie.Update(true, true);
                }), System.Windows.Threading.DispatcherPriority.Background);
            }
        }

        private void btnSetChart_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null)
            {
                _tipoGrafico = int.Parse(btn.Tag.ToString()!);
                btnChartBar.Background  = AppBrushes.BotaoAtivo(_tipoGrafico == 0);
                btnChartLine.Background = AppBrushes.BotaoAtivo(_tipoGrafico == 1);
                btnChartPie.Background  = AppBrushes.BotaoAtivo(_tipoGrafico == 2);
                CarregarDados();
            }
        }

        // ─── KPI cards ───────────────────────────────────────────────────────────
        private void AtualizarIndicadores(
            List<(string Periodo, decimal Receitas, decimal Despesas)> dados,
            List<(string Categoria, decimal Total)> gastosPorCategoria)
        {
            decimal totalR = dados.Sum(d => d.Receitas);
            decimal totalD = dados.Sum(d => d.Despesas);
            decimal balanco = totalR - totalD;

            // Poupança
            string statusPoupanca;
            if (balanco < 0)
                statusPoupanca = "Défice";
            else if (balanco < totalR * 0.1m)
                statusPoupanca = "Mínimo";
            else if (balanco < totalR * 0.3m)
                statusPoupanca = "Razoável";
            else
                statusPoupanca = "Excelente";

            txtStatusPoupanca.Text = statusPoupanca;
            txtStatusPoupanca.Foreground = balanco < 0 ? AppBrushes.Vermelho
                                         : balanco < totalR * 0.1m ? AppBrushes.Laranja
                                         : AppBrushes.Verde;

            string sinalBalanco = balanco >= 0 ? "+" : "";
            txtValorBalanco.Text = $"Balanço total: {sinalBalanco}{balanco:N2} €";

            // Estado da conta
            string estadoConta;
            double saudeScore;
            if (totalR == 0)
            {
                estadoConta = "Sem receitas";
                saudeScore = 0;
            }
            else if (totalD > totalR)
            {
                estadoConta = "Em Risco ⚠️";
                saudeScore = 10;
            }
            else if (totalD > totalR * 0.8m)
            {
                estadoConta = "Cuidado 🔔";
                saudeScore = 40;
            }
            else
            {
                estadoConta = "Saudável ✓";
                saudeScore = Math.Min(100, (double)((1 - totalD / totalR) * 100));
            }

            txtEstadoConta.Text = estadoConta;
            txtEstadoConta.Foreground = saudeScore >= 50 ? AppBrushes.Verde
                                      : saudeScore >= 25 ? AppBrushes.Laranja
                                      : AppBrushes.Vermelho;

            if (pbSaude != null)
            {
                pbSaude.Value = saudeScore;
                pbSaude.Foreground = saudeScore >= 50 ? AppBrushes.Verde
                                   : saudeScore >= 25 ? AppBrushes.Laranja
                                   : AppBrushes.Vermelho;
            }

            // Maior gasto por categoria
            var maiorGasto = gastosPorCategoria.OrderByDescending(g => g.Total).FirstOrDefault();
            txtMaiorGasto.Text = maiorGasto.Categoria ?? "---";
        }


        // ─── Export Excel (CSV melhorado) ─────────────────────────────────────────
        private void btnExportar_Click(object sender, RoutedEventArgs e)
        {
            if (Sessao.UtilizadorAtual == null) return;

            UiHelper.ExecutarComTratamento(() =>
            {
                var save = new SaveFileDialog
                {
                    Filter   = "Ficheiro Excel CSV (*.csv)|*.csv",
                    FileName = $"DailyBudget_Relatorio_{DateTime.Now:yyyyMMdd}"
                };
                if (save.ShowDialog() != true) return;

                int userId = Sessao.UtilizadorAtual.Id;
                var receitas = RepositorioTransacoes.ObterReceitas(userId);
                var despesas = RepositorioTransacoes.ObterDespesas(userId);

                decimal totalR = receitas.Sum(r => r.Valor);
                decimal totalD = despesas.Sum(d => d.Valor);
                decimal saldo  = totalR - totalD;

                var sb = new StringBuilder();
                // BOM para Excel reconhecer UTF-8
                sb.Append('\uFEFF');

                // ── Cabeçalho do relatório ──
                sb.AppendLine("DAILYBUDGET - RELATÓRIO FINANCEIRO");
                sb.AppendLine($"Gerado em;{DateTime.Now:dd/MM/yyyy HH:mm}");
                sb.AppendLine($"Utilizador;{Sessao.UtilizadorAtual.Nome}");
                sb.AppendLine();

                // ── Resumo ──
                sb.AppendLine("=== RESUMO ===");
                sb.AppendLine($"Total Receitas;{totalR:N2} €");
                sb.AppendLine($"Total Despesas;{totalD:N2} €");
                sb.AppendLine($"Saldo Líquido;{saldo:N2} €");
                sb.AppendLine();

                // ── Receitas ──
                sb.AppendLine("=== RECEITAS ===");
                sb.AppendLine("Data;Descrição;Valor (€)");
                foreach (var r in receitas.OrderBy(x => x.Data))
                    sb.AppendLine($"{r.Data:dd/MM/yyyy};{r.Descricao};{r.Valor:N2}");
                sb.AppendLine($";;TOTAL: {totalR:N2}");
                sb.AppendLine();

                // ── Despesas ──
                sb.AppendLine("=== DESPESAS ===");
                sb.AppendLine("Data;Descrição;Categoria;Valor (€)");
                foreach (var d in despesas.OrderBy(x => x.Data))
                    sb.AppendLine($"{d.Data:dd/MM/yyyy};{d.Descricao};{d.Categoria};{d.Valor:N2}");
                sb.AppendLine($";;;TOTAL: {totalD:N2}");
                sb.AppendLine();

                // ── Gastos por categoria ──
                var gastosCat = RepositorioTransacoes.ObterGastosPorCategoria(userId);
                if (gastosCat.Count > 0)
                {
                    sb.AppendLine("=== GASTOS POR CATEGORIA ===");
                    sb.AppendLine("Categoria;Total (€);% do Total");
                    foreach (var g in gastosCat.OrderByDescending(x => x.Total))
                    {
                        double pct = totalD > 0 ? (double)(g.Total / totalD * 100) : 0;
                        sb.AppendLine($"{g.Categoria};{g.Total:N2};{pct:0.0}%");
                    }
                    sb.AppendLine();
                }

                File.WriteAllText(save.FileName, sb.ToString(), new UTF8Encoding(true));
                CaixaMensagem.Mostrar(
                    $"Relatório exportado com sucesso!\n\nFicheiro: {Path.GetFileName(save.FileName)}",
                    "Exportado!", TipoMensagem.Sucesso);

            }, "Erro ao exportar o relatório.");
        }
    }
}
