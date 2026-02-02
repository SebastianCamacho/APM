using UI.ViewModels;

namespace UI.Views
{
    public partial class HomePage : ContentPage
    {
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
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            if (BindingContext is HomeViewModel viewModel)
            {
                viewModel.StopMonitoring();
            }
        }
    }
}