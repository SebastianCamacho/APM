using UI.ViewModels;

namespace UI.Views
{
    public partial class PrintersPage : ContentPage
    {
        private bool _isAnimating = false;

        public PrintersPage(PrintersViewModel viewModel)
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

            if (BindingContext is PrintersViewModel viewModel)
            {
                viewModel.LoadPrintersCommand.Execute(null);
                viewModel.StartMonitoring();
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _isAnimating = false;

            if (BindingContext is PrintersViewModel viewModel)
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

                aura.Scale = 1;
                aura.Opacity = 0.6;

                await Task.WhenAll(
                    aura.ScaleToAsync(2.5, 1200, Easing.SinOut),
                    aura.FadeToAsync(0, 1200, Easing.SinOut)
                );

                await Task.Delay(400);
            }
        }

        // --- Lógica manual para el Hover de los íconos de Acción (Edit/Delete) ---
        // Dado que MAUI VisualStateManager ignora la directiva Setter TargetName="" dentro de DataTemplates, 
        // lo controlamos manualmente inyectando eventos Pointer.

        private void OnCardPointerEntered(object sender, PointerEventArgs e)
        {
            // Solo en Windows/Mac ocultamos iconos por defecto y los mostramos en hover
#if WINDOWS || MACCATALYST
            if (sender is Border border)
            {
                // Buscar el contenedor de acciones ("ActionIcons")
                var actionIcons = border.FindByName<HorizontalStackLayout>("ActionIcons");
                if (actionIcons != null)
                {
                    // Cancelar cualquier Fade Out (opacidad en curso) fantasma
                    actionIcons.CancelAnimations();
                    
                    actionIcons.IsVisible = true;
                    actionIcons.FadeTo(1, 150);
                }
            }
#endif
        }

        private void OnCardPointerExited(object sender, PointerEventArgs e)
        {
#if WINDOWS || MACCATALYST
            if (sender is Border border)
            {
                var actionIcons = border.FindByName<HorizontalStackLayout>("ActionIcons");
                if (actionIcons != null)
                {
                    // Cancelar cualquier Fade In en progreso antes de ocultar
                    actionIcons.CancelAnimations();

                    actionIcons.FadeTo(0, 150).ContinueWith(t => {
                        // Importante: No apagar 'IsVisible' si el Opacity volvió a 1 por un re-ingreso súper rápido
                        MainThread.BeginInvokeOnMainThread(() => 
                        {
                            if (actionIcons.Opacity == 0)
                            {
                                actionIcons.IsVisible = false;
                            }
                        });
                    });
                }
            }
#endif
        }

        // --- Swapping Dinámico para los Botones (Windows) ---

        private void OnEditPointerEntered(object sender, PointerEventArgs e)
        {
#if WINDOWS || MACCATALYST
            if (sender is ImageButton btn)
            {
                btn.Source = "icono_edit.png";
                
                // Extraer Color PrimaryTintBg según el tema actual (Light/Dark)
                if (Application.Current != null && Application.Current.Resources.TryGetValue("PrimaryTintBgLight", out var colorL) && Application.Current.Resources.TryGetValue("PrimaryTintBgDark", out var colorD))
                {
                   btn.BackgroundColor = (Application.Current.RequestedTheme == AppTheme.Dark) ? (Color)colorD : (Color)colorL;
                }
            }
#endif
        }

        private void OnEditPointerExited(object sender, PointerEventArgs e)
        {
#if WINDOWS || MACCATALYST
            if (sender is ImageButton btn)
            {
                btn.Source = "icono_edit_gray.png";
                btn.BackgroundColor = Colors.Transparent;
            }
#endif
        }

        private void OnDeletePointerEntered(object sender, PointerEventArgs e)
        {
#if WINDOWS || MACCATALYST
            if (sender is ImageButton btn)
            {
                btn.Source = "icono_delete.png";
                
                if (Application.Current != null && Application.Current.Resources.TryGetValue("DangerTintBgLight", out var colorL) && Application.Current.Resources.TryGetValue("DangerTintBgDark", out var colorD))
                {
                   btn.BackgroundColor = (Application.Current.RequestedTheme == AppTheme.Dark) ? (Color)colorD : (Color)colorL;
                }
            }
#endif
        }

        private void OnDeletePointerExited(object sender, PointerEventArgs e)
        {
#if WINDOWS || MACCATALYST
            if (sender is ImageButton btn)
            {
                btn.Source = "icono_delete_gray.png";
                btn.BackgroundColor = Colors.Transparent;
            }
#endif
        }
    }
}
