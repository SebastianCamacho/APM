using UI.ViewModels;

namespace UI.Views
{
    public partial class SettingsPage : ContentPage
    {
        private readonly SettingsViewModel _viewModel;

        public SettingsPage(SettingsViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            _viewModel.StartMonitoring();
            await _viewModel.LoadTemplatesAsync();
            PulseAnimation();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _viewModel.StopMonitoring();
            _isPulsing = false;
        }

        private bool _isPulsing;

        private async void PulseAnimation()
        {
            if (_isPulsing) return;
            _isPulsing = true;

            var pulseIndicator = (Microsoft.Maui.Controls.Shapes.Ellipse)this.FindByName("PulseIndicator");
            if (pulseIndicator == null) return;

            while (_isPulsing)
            {
                await pulseIndicator.ScaleToAsync(1.5, 800, Easing.CubicOut);
                await pulseIndicator.FadeToAsync(0, 800, Easing.CubicOut);

                pulseIndicator.Scale = 1;
                pulseIndicator.Opacity = 0.5;

                await Task.Delay(200);
            }
        }
    }
}
