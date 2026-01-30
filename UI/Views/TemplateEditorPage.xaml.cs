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
        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (BindingContext is TemplateEditorViewModel vm)
            {
                vm.LoadDataCommand.Execute(null);
            }
        }
    }
}
