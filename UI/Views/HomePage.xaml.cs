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
            (BindingContext as HomeViewModel)?.StartMonitoring();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            (BindingContext as HomeViewModel)?.StopMonitoring();
        }
    }
}