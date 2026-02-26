using UI.ViewModels;

namespace UI.Views
{
    public partial class LogsPage : ContentPage
    {
        public LogsPage(LogsViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}