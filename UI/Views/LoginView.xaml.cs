namespace UI.Views
{
    public partial class LoginView : ContentPage
    {
        public LoginView(ViewModels.LoginViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
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