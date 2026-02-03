using UI.ViewModels;

namespace UI.Views
{
    public partial class ScalesPage : ContentPage
    {
        public ScalesPage(ScalesViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (BindingContext is ScalesViewModel viewModel)
            {
                viewModel.LoadScalesCommand.Execute(null);
            }
        }
    }
}
