using DailyBudgetWPF.Dados.Repositorios;
using DailyBudgetWPF.Dados;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DailyBudgetWPF.Modelos;
using System.Threading.Tasks;
using DailyBudgetWPF.Helpers;

namespace DailyBudgetWPF.Vistas
{
    public partial class Categorias : UserControl
    {
        private string _emojiSelecionado = "💰";
        private string[] emojisPredefinidos = {
            "💰", "🍔", "🚗", "🎮", "🏠", "💊", "🛒", "👔", "🎓",
            "💇", "🚲", "✈️", "🎭", "🔌", "📶", "📱", "🎁", "🧹",
            "💼", "🎵", "⚽", "🐶", "🐱", "🌿", "🍕", "🚂", "📚",
            "💚", "🏥", "📦", "🌺", "⚡", "🔥", "❤️", "⭐", "🎉"
        };
        
        // Estado do Color Picker HSV (inicializado com a cor #2ECC71)
        private double _selectedHue = 145;
        private double _selectedSaturation = 0.77;
        private double _selectedValue = 0.80;

        public Categorias()
        {
            InitializeComponent();
            // Popular grelha de emojis no popup
            var estilo = (Style)FindResource("EstiloBotaoEmoji");
            foreach (var emoji in emojisPredefinidos)
            {
                var btn = new Button
                {
                    Content = new TextBlock
                    {
                        Text = emoji,
                        FontFamily = new FontFamily("Segoe UI Emoji"),
                        FontSize = 22,
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Center
                    },
                    Style = estilo,
                    Tag = emoji
                };
                btn.Click += (s, e) =>
                {
                    _emojiSelecionado = ((Button)s).Tag?.ToString() ?? "💰";
                    txtEmojiSelecionado.Text = _emojiSelecionado;
                    popEmojis.IsOpen = false;
                };
                wpEmojis.Children.Add(btn);
            }
            CarregarDados();
        }

        private async void CarregarDados()
        {
            try
            {
                var cats = await Task.Run(() => RepositorioCategorias.ObterTodas());
                var orcs = Sessao.UtilizadorAtual != null
                    ? await Task.Run(() => RepositorioOrcamentos.ObterOrcamentos(Sessao.UtilizadorAtual.Id))
                    : null;

                // Volta automaticamente ao UI thread após o await
                icCategorias.ItemsSource = cats;
                cbCatOrcamento.ItemsSource = cats;
                cbCatOrcamento.DisplayMemberPath = "NomeExibicao";
                cbCatOrcamento.SelectedValuePath = "Id";
                if (cbCatOrcamento.Items.Count > 0) cbCatOrcamento.SelectedIndex = 0;
                if (orcs != null) icOrcamentos.ItemsSource = orcs;
            }
            catch (Exception ex)
            {
                CaixaMensagem.Mostrar($"Erro ao carregar dados: {ex.Message}", "Erro", TipoMensagem.Erro);
            }
        }

        private async void CarregarCategorias()
        {
            try
            {
                var cats = await Task.Run(() => RepositorioCategorias.ObterTodas());
                icCategorias.ItemsSource = cats;
                cbCatOrcamento.ItemsSource = cats;
                cbCatOrcamento.DisplayMemberPath = "NomeExibicao";
                cbCatOrcamento.SelectedValuePath = "Id";
                if (cbCatOrcamento.Items.Count > 0) cbCatOrcamento.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                CaixaMensagem.Mostrar($"Erro ao carregar categorias: {ex.Message}", "Erro", TipoMensagem.Erro);
            }
        }

        private async void CarregarOrcamentos()
        {
            if (Sessao.UtilizadorAtual == null) return;
            try
            {
                var orcs = await Task.Run(() => RepositorioOrcamentos.ObterOrcamentos(Sessao.UtilizadorAtual.Id));
                icOrcamentos.ItemsSource = orcs;
            }
            catch (Exception ex)
            {
                CaixaMensagem.Mostrar($"Erro ao carregar orçamentos: {ex.Message}", "Erro", TipoMensagem.Erro);
            }
        }

        private async void btnAdicionarCategoria_Click(object sender, RoutedEventArgs e)
        {
            string nome = txtNomeCategoria.Text;
            if (string.IsNullOrWhiteSpace(nome)) { CaixaMensagem.Mostrar("Insira um nome para a categoria.", "Aviso", TipoMensagem.Aviso); return; }
            string emoji = _emojiSelecionado;
            string cor = txtHexSelecionado.Text;
            try
            {
                await Task.Run(() => RepositorioCategorias.Adicionar(nome, emoji, cor));
                txtNomeCategoria.Clear();
                await CarregarCategoriasAsync();
            }
            catch (Exception ex)
            {
                CaixaMensagem.Mostrar($"Erro ao adicionar categoria: {ex.Message}", "Erro", TipoMensagem.Erro);
            }
        }

        private async void btnRemoverCategoria_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                try
                {
                    await Task.Run(() => RepositorioCategorias.Remover(id));
                    await CarregarCategoriasAsync();
                    await CarregarOrcamentosAsync();
                }
                catch (Exception ex)
                {
                    CaixaMensagem.Mostrar(ex.Message, "Aviso", TipoMensagem.Aviso);
                }
            }
        }

        private async void btnDefinirOrcamento_Click(object sender, RoutedEventArgs e)
        {
            if (Sessao.UtilizadorAtual == null) return;
            if (cbCatOrcamento.SelectedValue is not int catId) return;
            if (!UiHelper.TentarConverterDecimal(txtLimiteMensal.Text, out decimal limite) || limite <= 0)
            { CaixaMensagem.Mostrar("Insira um limite mensal válido (ex: 200,00).", "Aviso", TipoMensagem.Aviso); return; }
            try
            {
                await Task.Run(() => RepositorioOrcamentos.DefinirOrcamento(Sessao.UtilizadorAtual.Id, catId, limite));
                txtLimiteMensal.Clear();
                await CarregarOrcamentosAsync();
                CaixaMensagem.Mostrar("Orçamento definido com sucesso!", "Sucesso", TipoMensagem.Sucesso);
            }
            catch (Exception ex)
            {
                CaixaMensagem.Mostrar($"Erro ao definir orçamento: {ex.Message}", "Erro", TipoMensagem.Erro);
            }
        }

        private void txtLimiteMensal_LostFocus(object sender, RoutedEventArgs e)
        {
            UiHelper.FormatarTextBoxDecimal(txtLimiteMensal);
        }

        private async void btnRemoverOrcamento_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                try
                {
                    await Task.Run(() => RepositorioOrcamentos.RemoverOrcamento(id));
                    await CarregarOrcamentosAsync();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erro ao remover orçamento: {ex.Message}");
                }
            }
        }

        // Versões awaitable para uso interno
        private async Task CarregarCategoriasAsync()
        {
            var cats = await Task.Run(() => RepositorioCategorias.ObterTodas());
            icCategorias.ItemsSource = cats;
            cbCatOrcamento.ItemsSource = cats;
            cbCatOrcamento.DisplayMemberPath = "NomeExibicao";
            cbCatOrcamento.SelectedValuePath = "Id";
            if (cbCatOrcamento.Items.Count > 0) cbCatOrcamento.SelectedIndex = 0;
        }

        private async Task CarregarOrcamentosAsync()
        {
            if (Sessao.UtilizadorAtual == null) return;
            var orcs = await Task.Run(() => RepositorioOrcamentos.ObterOrcamentos(Sessao.UtilizadorAtual.Id));
            icOrcamentos.ItemsSource = orcs;
        }

        // ──────────────────────────────────────────────
        // Interatividade do Color Picker Personalizado
        // ──────────────────────────────────────────────
        private void btnAbrirColorPicker_Click(object sender, RoutedEventArgs e)
        {
            popColorPicker.IsOpen = !popColorPicker.IsOpen;
            if (popColorPicker.IsOpen)
            {
                AtualizarPosicaoSeletorEPixel();
            }
        }

        private void btnAbrirEmojis_Click(object sender, RoutedEventArgs e)
        {
            popEmojis.IsOpen = !popEmojis.IsOpen;
        }

        private void brdColorCanvas_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            brdColorCanvas.CaptureMouse();
            ProcessarMouseCanvas(e);
        }

        private void brdColorCanvas_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (brdColorCanvas.IsMouseCaptured)
            {
                ProcessarMouseCanvas(e);
            }
        }

        private void brdColorCanvas_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            brdColorCanvas.ReleaseMouseCapture();
        }

        private void ProcessarMouseCanvas(System.Windows.Input.MouseEventArgs e)
        {
            Point p = e.GetPosition(brdColorCanvas);
            double w = brdColorCanvas.ActualWidth;
            double h = brdColorCanvas.ActualHeight;

            double x = Math.Max(0, Math.Min(p.X, w));
            double y = Math.Max(0, Math.Min(p.Y, h));

            _selectedSaturation = x / w;
            _selectedValue = 1.0 - (y / h);

            Canvas.SetLeft(elpCanvasSelector, x);
            Canvas.SetTop(elpCanvasSelector, y);

            AtualizarPreviewColor();
        }

        private void sldHue_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (brdCanvasHueBase == null) return;
            _selectedHue = sldHue.Value;
            Color baseColor = ColorFromHSV(_selectedHue, 1.0, 1.0);
            brdCanvasHueBase.Background = new SolidColorBrush(baseColor);
            AtualizarPreviewColor();
        }

        private void btnConfirmarCor_Click(object sender, RoutedEventArgs e)
        {
            Color selectedColor = ColorFromHSV(_selectedHue, _selectedSaturation, _selectedValue);
            string hex = $"#{selectedColor.R:X2}{selectedColor.G:X2}{selectedColor.B:X2}";

            brdCorSelecionada.Background = new SolidColorBrush(selectedColor);
            txtHexSelecionado.Text = hex;
            popColorPicker.IsOpen = false;
        }

        private void AtualizarPosicaoSeletorEPixel()
        {
            double w = brdColorCanvas.ActualWidth > 0 ? brdColorCanvas.ActualWidth : 190;
            double h = brdColorCanvas.ActualHeight > 0 ? brdColorCanvas.ActualHeight : 120;

            double x = _selectedSaturation * w;
            double y = (1.0 - _selectedValue) * h;

            Canvas.SetLeft(elpCanvasSelector, x);
            Canvas.SetTop(elpCanvasSelector, y);

            Color baseColor = ColorFromHSV(_selectedHue, 1.0, 1.0);
            brdCanvasHueBase.Background = new SolidColorBrush(baseColor);

            AtualizarPreviewColor();
        }

        private void AtualizarPreviewColor()
        {
            Color selectedColor = ColorFromHSV(_selectedHue, _selectedSaturation, _selectedValue);
            string hex = $"#{selectedColor.R:X2}{selectedColor.G:X2}{selectedColor.B:X2}";

            if (brdPreviewColor != null)
                brdPreviewColor.Background = new SolidColorBrush(selectedColor);
            if (txtHexPreview != null)
                txtHexPreview.Text = hex;
        }

        public static Color ColorFromHSV(double hue, double saturation, double value)
        {
            int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            double f = hue / 60 - Math.Floor(hue / 60);

            value = value * 255;
            byte v = Convert.ToByte(Math.Min(255, Math.Max(0, value)));
            byte p = Convert.ToByte(Math.Min(255, Math.Max(0, value * (1 - saturation))));
            byte q = Convert.ToByte(Math.Min(255, Math.Max(0, value * (1 - f * saturation))));
            byte t = Convert.ToByte(Math.Min(255, Math.Max(0, value * (1 - (1 - f) * saturation))));

            if (hi == 0) return Color.FromRgb(v, t, p);
            else if (hi == 1) return Color.FromRgb(q, v, p);
            else if (hi == 2) return Color.FromRgb(p, v, t);
            else if (hi == 3) return Color.FromRgb(p, q, v);
            else if (hi == 4) return Color.FromRgb(t, p, v);
            else return Color.FromRgb(v, p, q);
        }

        private void lstEmojis_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            // Mantido para compatibilidade, mas não usado
        }

        private void scrMain_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            // Se não houve mudança de scroll real (apenas layout/extent), não faz nada
            if (e.VerticalChange == 0 && e.HorizontalChange == 0)
                return;

            if (popEmojis != null && popEmojis.IsOpen && !popEmojis.IsMouseOver)
                popEmojis.IsOpen = false;

            if (popColorPicker != null && popColorPicker.IsOpen && !popColorPicker.IsMouseOver)
                popColorPicker.IsOpen = false;

            if (btnAjudaCategorias != null && btnAjudaCategorias.IsChecked == true && !btnAjudaCategorias.IsMouseOver)
                btnAjudaCategorias.IsChecked = false;

            if (btnAjudaLimites != null && btnAjudaLimites.IsChecked == true && !btnAjudaLimites.IsMouseOver)
                btnAjudaLimites.IsChecked = false;
        }
    }
}
