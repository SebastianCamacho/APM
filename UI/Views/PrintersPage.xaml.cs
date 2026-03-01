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
                // Buscar el StackLayout de acciones ("ActionIcons")
                var actionIcons = border.FindByName<HorizontalStackLayout>("ActionIcons");
                if (actionIcons != null)
                {
                    // Transición suave
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
                    actionIcons.FadeTo(0, 150);
                }
            }
#endif
        }
    }
}
