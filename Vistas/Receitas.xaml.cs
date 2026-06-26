using DailyBudgetWPF.Dados.Repositorios;
using DailyBudgetWPF.Dados;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using DailyBudgetWPF.Modelos;
using DailyBudgetWPF.Helpers;

namespace DailyBudgetWPF.Vistas
{
    public partial class Receitas : UserControl
    {
        private int _idEmEdicao = -1;

        public Receitas()
        {
            InitializeComponent();
            dpData.SelectedDate = DateTime.Now;
            CarregarReceitas();
        }

        private void CarregarReceitas(DateTime? dataFiltro = null)
        {
            if (Sessao.UtilizadorAtual == null) return;
            var de  = dataFiltro.HasValue ? dataFiltro.Value.Date          : (DateTime?)null;
            var ate = dataFiltro.HasValue ? dataFiltro.Value.Date.AddDays(1).AddSeconds(-1) : (DateTime?)null;
            var lista = RepositorioTransacoes.ObterReceitasFiltradas(Sessao.UtilizadorAtual.Id, de, ate);
            dgReceitas.ItemsSource = lista;
            if (txtResultadoFiltro != null)
                txtResultadoFiltro.Text = dataFiltro.HasValue
                    ? $"{lista.Count} receita(s) em {dataFiltro.Value:dd/MM/yyyy}"
                    : string.Empty;
        }

        private void btnGuardar_Click(object sender, RoutedEventArgs e)
        {
            if (Sessao.UtilizadorAtual == null) return;
            string desc = txtDescricao.Text;
            if (string.IsNullOrWhiteSpace(desc)) { CaixaMensagem.Mostrar("Insira uma descrição.", "Aviso", TipoMensagem.Aviso); return; }

            if (!UiHelper.TentarConverterDecimal(txtValor.Text, out decimal valor) || valor <= 0)
            { CaixaMensagem.Mostrar("Insira um valor válido (ex: 150,00).", "Aviso", TipoMensagem.Aviso); return; }

            DateTime data = dpData.SelectedDate ?? DateTime.Now;

            if (_idEmEdicao > 0)
            {
                RepositorioTransacoes.EditarReceita(_idEmEdicao, desc, valor, data);
                CancelarEdicao();
            }
            else
            {
                RepositorioTransacoes.AdicionarReceita(Sessao.UtilizadorAtual.Id, desc, valor, data);
                txtDescricao.Clear();
                txtValor.Clear();
            }
            CarregarReceitas();
        }

        private void btnEditar_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Transacao item)
            {
                _idEmEdicao = item.Id;
                txtDescricao.Text = item.Descricao;
                txtValor.Text = item.Valor.ToString("N2", new CultureInfo("pt-PT"));
                dpData.SelectedDate = item.Data;
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
            btnGuardar.Content = "REGISTAR";
            btnCancelar.Visibility = Visibility.Collapsed;
        }

        private void btnRemover_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                if (CaixaMensagem.Confirmar("Tem a certeza que deseja remover esta receita?", "Confirmar Remoção"))
                {
                    RepositorioTransacoes.RemoverReceita(id);
                    CarregarReceitas();
                }
            }
        }

        private void txtValor_LostFocus(object sender, RoutedEventArgs e)
        {
            UiHelper.FormatarTextBoxDecimal(txtValor);
        }

        private void dpFiltro_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            CarregarReceitas(dpFiltro.SelectedDate);
        }

        private void btnLimparFiltro_Click(object sender, RoutedEventArgs e)
        {
            dpFiltro.SelectedDate = null;
            CarregarReceitas(null);
        }
    }
}
