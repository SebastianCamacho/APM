using UI.ViewModels;

namespace UI.Views
{
    public partial class PrinterDetailPage : ContentPage
    {
        public PrinterDetailPage(PrinterDetailViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}
