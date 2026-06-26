using DailyBudgetWPF.Dados.Repositorios;
using DailyBudgetWPF.Dados;
using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using DailyBudgetWPF.Modelos;
using DailyBudgetWPF.Helpers;

namespace DailyBudgetWPF.Vistas
{
    public partial class ListaDesejos : UserControl
    {
        public ListaDesejos()
        {
            InitializeComponent();
            CarregarItens();
        }

        private void CarregarItens()
        {
            if (Sessao.UtilizadorAtual == null) return;
            try
            {
                int userId = Sessao.UtilizadorAtual.Id;
                decimal saldoAtual = RepositorioTransacoes.ObterTotalReceitas(userId)
                                   - RepositorioTransacoes.ObterTotalDespesas(userId);

                var itens = RepositorioDesejos.ObterItens(userId);
                // Injetar saldo atual em cada item para a barra de progresso
                foreach (var item in itens)
                    item.SaldoAtual = saldoAtual;

                icWishlist.ItemsSource = itens;
                AtualizarResumo(itens, saldoAtual);
                AtualizarEstadoVisual(itens.Count);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro na wishlist: {ex.Message}");
            }
        }

        private void AtualizarEstadoVisual(int totalItens)
        {
            if (totalItens == 0)
            {
                bordEstadoVazio.Visibility = Visibility.Visible;
                bordListaItens.Visibility = Visibility.Collapsed;
            }
            else
            {
                bordEstadoVazio.Visibility = Visibility.Collapsed;
                bordListaItens.Visibility = Visibility.Visible;
                txtContador.Text = totalItens == 1 ? "1 item" : $"{totalItens} itens";
            }
        }

        private void AtualizarResumo(System.Collections.Generic.List<ItemDesejado> itens, decimal saldo)
        {
            int adquiridos = itens.FindAll(i => i.Adquirido).Count;
            decimal totalDesejado = 0;
            foreach (var i in itens) if (!i.Adquirido) totalDesejado += i.ValorEstimado;

            txtSaldoResumo.Text = $"{saldo:N2} €";
            txtAdquiridosResumo.Text = $"{adquiridos} / {itens.Count}";
            txtFaltaPouparResumo.Text = $"{totalDesejado:N2} €";
        }

        private void btnAdicionarItem_Click(object sender, RoutedEventArgs e)
        {
            if (Sessao.UtilizadorAtual == null) return;
            string item = txtItem.Text;
            if (string.IsNullOrWhiteSpace(item))
            {
                CaixaMensagem.Mostrar("Indica o nome do item que desejas.", "Campo obrigatório", TipoMensagem.Aviso);
                return;
            }

            UiHelper.TentarConverterDecimal(txtPreco.Text, out decimal preco);

            if (RepositorioDesejos.AdicionarItem(Sessao.UtilizadorAtual.Id, item, preco, 1))
            {
                txtItem.Clear();
                txtPreco.Clear();
                CarregarItens();
            }
        }

        private void btnDone_Click(object sender, RoutedEventArgs e)
        {
            if (Sessao.UtilizadorAtual == null) return;
            if (sender is Button btn && btn.Tag is ItemDesejado itemDesejado)
            {
                if (itemDesejado.Adquirido) return; // já marcado

                // ── Verificação de saldo ──────────────────────────────────
                int userId = Sessao.UtilizadorAtual.Id;
                decimal saldoAtual = RepositorioTransacoes.ObterTotalReceitas(userId)
                                   - RepositorioTransacoes.ObterTotalDespesas(userId);
                decimal saldoApos  = saldoAtual - itemDesejado.ValorEstimado;

                if (saldoApos < 0)
                {
                    // Saldo fica negativo: avisa e pede confirmação
                    bool prosseguir = CaixaMensagem.Confirmar(
                        $"⚠️ O teu saldo atual é {saldoAtual:N2} €.\n" +
                        $"Comprar \"{itemDesejado.Item}\" ({itemDesejado.ValorEstimado:N2} €) faria o teu saldo ficar NEGATIVO ({saldoApos:N2} €)!\n\n" +
                        "Tens a certeza que queres continuar?",
                        "🚨 Saldo Insuficiente");
                    if (!prosseguir) return;
                }
                else if (saldoApos == 0)
                {
                    // Saldo fica exatamente zero: avisa mas permite sem confirmação extra
                    bool prosseguir = CaixaMensagem.Confirmar(
                        $"⚠️ Esta compra vai deixar o teu saldo em 0,00 €!\n" +
                        "Queres mesmo continuar?",
                        "⚠️ Saldo Zero");
                    if (!prosseguir) return;
                }

                // Marcar como adquirido na wishlist
                RepositorioDesejos.MarcarComoAdquirido(itemDesejado.Id);

                // Registar automaticamente como despesa
                try
                {
                    var categorias = RepositorioCategorias.ObterTodas();
                    var cat = categorias.FirstOrDefault(c =>
                                  c.Nome.Contains("Lazer", StringComparison.OrdinalIgnoreCase) ||
                                  c.Nome.Contains("Compras", StringComparison.OrdinalIgnoreCase) ||
                                  c.Nome.Contains("Desejos", StringComparison.OrdinalIgnoreCase))
                              ?? categorias.FirstOrDefault();

                    if (cat != null && itemDesejado.ValorEstimado > 0)
                    {
                        RepositorioTransacoes.AdicionarDespesa(
                            userId,
                            $"🛍️ {itemDesejado.Item}",
                            itemDesejado.ValorEstimado,
                            DateTime.Today,
                            cat.Id);

                        CaixaMensagem.Mostrar(
                            $"✅ \"{itemDesejado.Item}\" marcado como comprado!\n💸 Despesa de {itemDesejado.ValorEstimado:N2} € registada em \"{cat.Nome}\".\nSaldo atual: {saldoApos:N2} €",
                            "Desejo Realizado!",
                            TipoMensagem.Sucesso);
                    }
                    else
                    {
                        CaixaMensagem.Mostrar(
                            $"✅ \"{itemDesejado.Item}\" marcado como adquirido!",
                            "Desejo Realizado!",
                            TipoMensagem.Sucesso);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erro ao registar despesa da wishlist: {ex.Message}");
                }

                CarregarItens();
            }
        }

        private void btnRemoverItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                if (CaixaMensagem.Confirmar("Tens a certeza que queres remover este item?", "Confirmar"))
                {
                    RepositorioDesejos.RemoverItem(id);
                    CarregarItens();
                }
            }
        }

        private void txtPreco_LostFocus(object sender, RoutedEventArgs e)
        {
            UiHelper.FormatarTextBoxDecimal(txtPreco);
        }
    }
}
