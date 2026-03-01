using UI.ViewModels;

namespace UI.Views
{
    public partial class HomePage : ContentPage
    {
        private bool _isAnimating = false;

        public HomePage(HomeViewModel viewModel)
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

            if (BindingContext is HomeViewModel viewModel)
            {
                viewModel.StartMonitoring();
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _isAnimating = false;

            if (BindingContext is HomeViewModel viewModel)
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
    }
}