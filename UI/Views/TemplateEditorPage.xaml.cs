using UI.ViewModels;

namespace UI.Views
{
    public partial class TemplateEditorPage : ContentPage
    {
        public TemplateEditorPage(TemplateEditorViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}
