using UI.ViewModels;
using Microsoft.Maui.Controls;
using System.ComponentModel;

namespace UI.Views
{
    public partial class HomePage : ContentPage
    {
        private bool _isAnimating;

        public HomePage(HomeViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (BindingContext is HomeViewModel viewModel)
            {
                viewModel.StartMonitoring();
                viewModel.PropertyChanged += OnViewModelPropertyChanged;
                UpdateAnimationState();
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            if (BindingContext is HomeViewModel viewModel)
            {
                viewModel.StopMonitoring();
                viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            }
            StopAnimation();
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(HomeViewModel.IsServiceRunning))
            {
                UpdateAnimationState();
            }
        }

        private void UpdateAnimationState()
        {
            if (BindingContext is HomeViewModel viewModel && viewModel.IsServiceRunning)
            {
                StartAnimation();
            }
            else
            {
                StopAnimation();
            }
        }

        private async void StartAnimation()
        {
            if (_isAnimating) return;

            _isAnimating = true;

            var spinnerAndroid = this.FindByName<Microsoft.Maui.Controls.Shapes.Ellipse>("ServiceSpinner");
            var spinnerWin = this.FindByName<Microsoft.Maui.Controls.Shapes.Ellipse>("ServiceSpinnerWin");

            // Start Android Spinner if visible/present
            if (spinnerAndroid != null)
            {
                spinnerAndroid.Rotation = 0;
            }

            // Start Windows Spinner if visible/present
            if (spinnerWin != null)
            {
                spinnerWin.Rotation = 0;
            }

            while (_isAnimating)
            {
                var tasks = new List<Task>();

                if (spinnerAndroid != null)
                    tasks.Add(spinnerAndroid.RotateTo(360, 2000, Easing.Linear));

                if (spinnerWin != null)
                    tasks.Add(spinnerWin.RotateTo(360, 2000, Easing.Linear));

                // If no spinners are found, wait to prevent loop spin
                if (tasks.Count == 0)
                {
                    await Task.Delay(2000);
                    // Try to finding again? In case view loaded late?
                    if (spinnerAndroid == null) spinnerAndroid = this.FindByName<Microsoft.Maui.Controls.Shapes.Ellipse>("ServiceSpinner");
                    if (spinnerWin == null) spinnerWin = this.FindByName<Microsoft.Maui.Controls.Shapes.Ellipse>("ServiceSpinnerWin");
                    continue;
                }

                await Task.WhenAll(tasks);

                // Reset to 0
                if (spinnerAndroid != null) spinnerAndroid.Rotation = 0;
                if (spinnerWin != null) spinnerWin.Rotation = 0;
            }
        }

        private void StopAnimation()
        {
            _isAnimating = false;

            var spinnerAndroid = this.FindByName<Microsoft.Maui.Controls.Shapes.Ellipse>("ServiceSpinner");
            var spinnerWin = this.FindByName<Microsoft.Maui.Controls.Shapes.Ellipse>("ServiceSpinnerWin");

            spinnerAndroid?.CancelAnimations();
            spinnerWin?.CancelAnimations();
        }
    }
}