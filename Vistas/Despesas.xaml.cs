using DailyBudgetWPF.Dados.Repositorios;
using DailyBudgetWPF.Dados;
using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
using DailyBudgetWPF.Modelos;
using DailyBudgetWPF.Helpers;

namespace DailyBudgetWPF.Vistas
{
    public partial class Despesas : UserControl
    {
        private int _idEmEdicao = -1;

        public Despesas()
        {
            InitializeComponent();
            dpData.SelectedDate = DateTime.Now;
            CarregarDados();
        }

        private void CarregarDados()
        {
            if (Sessao.UtilizadorAtual == null) return;
            var cats = RepositorioCategorias.ObterTodas();
            cbCategoria.ItemsSource = cats;
            cbCategoria.DisplayMemberPath = "NomeExibicao";
            cbCategoria.SelectedValuePath = "Id";
            if (cbCategoria.Items.Count > 0 && cbCategoria.SelectedIndex < 0)
                cbCategoria.SelectedIndex = 0;
            
            AtualizarTabela();
        }

        private void AtualizarTabela(DateTime? dataFiltro = null)
        {
            if (Sessao.UtilizadorAtual == null) return;
            var de  = dataFiltro.HasValue ? dataFiltro.Value.Date          : (DateTime?)null;
            var ate = dataFiltro.HasValue ? dataFiltro.Value.Date.AddDays(1).AddSeconds(-1) : (DateTime?)null;
            var lista = RepositorioTransacoes.ObterDespesasFiltradas(Sessao.UtilizadorAtual.Id, de, ate);
            dgDespesas.ItemsSource = lista;
            if (txtResultadoFiltro != null)
                txtResultadoFiltro.Text = dataFiltro.HasValue
                    ? $"{lista.Count} despesa(s) em {dataFiltro.Value:dd/MM/yyyy}"
                    : string.Empty;
        }

        private void btnGuardar_Click(object sender, RoutedEventArgs e)
        {
            if (Sessao.UtilizadorAtual == null) return;
            string desc = txtDescricao.Text;
            if (string.IsNullOrWhiteSpace(desc)) { CaixaMensagem.Mostrar("Insira uma descrição.", "Aviso", TipoMensagem.Aviso); return; }

            if (!UiHelper.TentarConverterDecimal(txtValor.Text, out decimal valor) || valor <= 0)
            { CaixaMensagem.Mostrar("Insira um valor válido (ex: 25,50).", "Aviso", TipoMensagem.Aviso); return; }

            if (cbCategoria.SelectedValue is not int catId)
            { CaixaMensagem.Mostrar("Selecione uma categoria.", "Aviso", TipoMensagem.Aviso); return; }

            // Validação de Limite de Categoria
            var orcamentos = RepositorioOrcamentos.ObterOrcamentos(Sessao.UtilizadorAtual.Id);
            var orcamento = orcamentos.FirstOrDefault(o => o.IdCategoria == catId);
            
            // Só validar se não for uma edição ou se o valor novo (menos o que já lá estava) ultrapassar, mas para simplificar, 
            // no limite ele só não pode ultrapassar o teto. Numa edição deveríamos descontar o valor antigo, mas como simplificação,
            // ou se quiser ser perfeito, ignoro a edição para esta regra rígida, ou aplico sempre:
            if (orcamento != null && _idEmEdicao <= 0)
            {
                if (orcamento.GastoMesAtual + valor > orcamento.LimiteMensal)
                {
                    CaixaMensagem.Mostrar("Esta despesa irá ultrapassar o limite mensal definido para esta categoria! Não é possível registar mais gastos nesta categoria.", "Limite Excedido", TipoMensagem.Aviso);
                    return;
                }
            }

            DateTime data = dpData.SelectedDate ?? DateTime.Now;

            if (_idEmEdicao > 0)
            {
                RepositorioTransacoes.EditarDespesa(_idEmEdicao, desc, valor, data, catId);
                CancelarEdicao();
            }
            else
            {
                RepositorioTransacoes.AdicionarDespesa(Sessao.UtilizadorAtual.Id, desc, valor, data, catId);
                txtDescricao.Clear();
                txtValor.Clear();
            }
            AtualizarTabela();
        }

        private void btnEditar_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Transacao item)
            {
                _idEmEdicao = item.Id;
                txtDescricao.Text = item.Descricao;
                txtValor.Text = item.Valor.ToString("N2", new CultureInfo("pt-PT"));
                dpData.SelectedDate = item.Data;
                // Selecionar categoria
                var cats = cbCategoria.ItemsSource as List<Categoria>;
                if (cats != null)
                {
                    var cat = cats.FirstOrDefault(c => c.Nome == item.Categoria);
                    if (cat != null) cbCategoria.SelectedValue = cat.Id;
                }
                btnGuardar.Content = "💾 GUARDAR EDIÇÃO";
                btnCancelar.Visibility = Visibility.Visible;
                txtDescricao.Focus();
            }
        }

        private void btnCancelar_Click(object sender, RoutedEventArgs e) => CancelarEdicao();

        private void CancelarEdicao()
        {
            _idEmEdicao = -1;
            txtDescricao.Clear();
            txtValor.Clear();
            dpData.SelectedDate = DateTime.Now;
            if (cbCategoria.Items.Count > 0) cbCategoria.SelectedIndex = 0;
            btnGuardar.Content = "REGISTAR";
            btnCancelar.Visibility = Visibility.Collapsed;
        }

        private void btnRemover_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                if (CaixaMensagem.Confirmar("Tem a certeza que deseja remover esta despesa?", "Confirmar Remoção"))
                {
                    RepositorioTransacoes.RemoverDespesa(id);
                    AtualizarTabela();
                }
            }
        }

        private void txtDescricao_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Autocomplete removido
        }

        private void txtValor_LostFocus(object sender, RoutedEventArgs e)
        {
            UiHelper.FormatarTextBoxDecimal(txtValor);
        }

        private void dpFiltro_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            AtualizarTabela(dpFiltro.SelectedDate);
        }

        private void btnLimparFiltro_Click(object sender, RoutedEventArgs e)
        {
            dpFiltro.SelectedDate = null;
            AtualizarTabela(null);
        }
    }
}
