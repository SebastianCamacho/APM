namespace UI.Views
{
    public partial class LoginView : ContentPage
    {
        public LoginView()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (BindingContext is ViewModels.LoginViewModel viewModel)
            {
                await viewModel.CheckPermissionsAsync();
            }
        }
    }
}