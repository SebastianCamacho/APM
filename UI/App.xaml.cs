using Microsoft.Extensions.DependencyInjection; // Se mantiene por si era necesario antes, si no, se puede eliminar.

namespace UI
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}