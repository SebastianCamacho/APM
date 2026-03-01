using UI.ViewModels;

namespace UI.Views
{
    public partial class AboutPage : ContentPage
    {
        private bool _isAnimating = false;

        public AboutPage(AboutViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
#if WINDOWS
            StartPulseAnimation();
#endif

            if (BindingContext is AboutViewModel viewModel)
            {
                viewModel.StartMonitoring();
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _isAnimating = false;

            if (BindingContext is AboutViewModel viewModel)
            {
                viewModel.StopMonitoring();
            }
        }

        private async void StartPulseAnimation()
        {
            if (_isAnimating) return;
            _isAnimating = true;

            while (_isAnimating)
            {
                var aura = this.FindByName<BoxView>("StatusAura");
                if (aura == null) break;

                // Reiniciar estado inicial
                aura.Scale = 1;
                aura.Opacity = 0.6;

                // Animación expansiva y desvanecimiento
                await Task.WhenAll(
                    aura.ScaleToAsync(2.5, 1200, Easing.SinOut),
                    aura.FadeToAsync(0, 1200, Easing.SinOut)
                );

                // Pequeña pausa antes del siguiente pálpito
                await Task.Delay(400);
            }
        }
        // --- Lógica de Hover para los Botones ---

        private void OnSupportPointerEntered(object sender, PointerEventArgs e)
        {
#if WINDOWS
            if (sender is Button btn)
            {
                // Verde clarito basado en Success (#21C063)
                if (Application.Current != null && Application.Current.Resources.TryGetValue("Success", out var successColor))
                {
                    Color sCol = (Color)successColor;
                    btn.BackgroundColor = sCol.WithAlpha(0.1f); // 10% de opacidad
                    btn.BorderColor = sCol;
                    btn.TextColor = sCol;
                }
            }
#endif
        }

        private void OnSupportPointerExited(object sender, PointerEventArgs e)
        {
#if WINDOWS
            if (sender is Button btn)
            {
                btn.BackgroundColor = Colors.Transparent;
                
                // Restaurar colores originales según el tema
                if (Application.Current != null)
                {
                    bool isDark = Application.Current.RequestedTheme == AppTheme.Dark;
                    
                    if (Application.Current.Resources.TryGetValue(isDark ? "CardStrokeDark" : "CardStrokeLight", out var strokeColor))
                        btn.BorderColor = (Color)strokeColor;
                        
                    if (Application.Current.Resources.TryGetValue(isDark ? "TextSecondaryDark" : "TextSecondaryLight", out var textColor))
                        btn.TextColor = (Color)textColor;
                }
            }
#endif
        }

        private void OnWebsitePointerEntered(object sender, PointerEventArgs e)
        {
#if WINDOWS
            if (sender is Button btn)
            {
                // Un tono más oscuro del primary para el hover del botón sólido
                if (Application.Current != null && Application.Current.Resources.TryGetValue("Primary", out var primaryColor))
                {

                    btn.BackgroundColor = ((Color)primaryColor).WithLuminosity(0.35f); // Oscurecer un poco
                }
            }
#endif
        }

        private void OnWebsitePointerExited(object sender, PointerEventArgs e)
        {
#if WINDOWS
            if (sender is Button btn)
            {
                if (Application.Current != null && Application.Current.Resources.TryGetValue("Primary", out var primaryColor))
                {
                    btn.BackgroundColor = (Color)primaryColor;
                }
            }
#endif
        }
    }
}
