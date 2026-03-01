using UI.ViewModels;

namespace UI.Views
{
    public partial class PrintersPage : ContentPage
    {
        public PrintersPage(PrintersViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (BindingContext is PrintersViewModel viewModel)
            {
                viewModel.LoadPrintersCommand.Execute(null);
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
                    actionIcons.FadeTo(0, 150).ContinueWith(t => {
                        MainThread.BeginInvokeOnMainThread(() => actionIcons.IsVisible = false);
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
