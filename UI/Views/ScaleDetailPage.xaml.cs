using UI.ViewModels;

namespace UI.Views
{
    public partial class ScaleDetailPage : ContentPage
    {
        public ScaleDetailPage(ScaleDetailViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}
