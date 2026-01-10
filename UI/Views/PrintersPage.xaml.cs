using UI.ViewModels;

namespace UI.Views
{
    public partial class PrintersPage : ContentPage
    {
        public PrintersPage(PrintersViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (BindingContext is PrintersViewModel viewModel)
            {
                viewModel.LoadPrintersCommand.Execute(null);
            }
        }
    }
}
