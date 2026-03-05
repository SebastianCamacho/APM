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
#if WINDOWS
            StartPulseAnimation();
#endif
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _viewModel.StopMonitoring();
            _isAnimating = false;
        }

        private bool _isAnimating;

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
    }
}
