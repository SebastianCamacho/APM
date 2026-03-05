using UI.ViewModels;

namespace UI.Views
{
    public partial class ScaleDetailPage : ContentPage
    {
        public ScaleDetailPage(ScaleDetailViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }

        private async void OnRefreshPortsClicked(object sender, EventArgs e)
        {
            if (sender is Button btn)
            {
                // Animación visual de 360 grados
                // Gira el botón tomando el centro como eje
                await btn.RelRotateToAsync(360, 400, Easing.CubicInOut);

                // Ejecutamos el comando de cargar puertos en el VM subyacente
                if (BindingContext is ScaleDetailViewModel viewModel)
                {
                    viewModel.LoadPortsCommand.Execute(null);
                }
            }
        }
    }
}
