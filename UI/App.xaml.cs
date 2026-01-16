using UI.Views; // Añadir este using
using CommunityToolkit.Mvvm.Messaging; // Añadir este using para mensajería MVVM
using Microsoft.Maui.Controls; // Añadir este using para Application.Current
using AppsielPrintManager.Core.Interfaces; // Add this using for IWorkerServiceManager
using Microsoft.Extensions.DependencyInjection; // Add this using for IServiceProvider
using System.Threading.Tasks;

namespace UI
{
    public partial class App : Application
    {
        private readonly IServiceProvider _serviceProvider;
        private IWorkerServiceManager _workerServiceManager;
        private ITrayAppService _trayAppService; // Nuevo: para el servicio TrayApp

        public App(IServiceProvider serviceProvider)
        {
            InitializeComponent();

            _serviceProvider = serviceProvider;
            UserAppTheme = AppTheme.Light;

            // Suscribirse al mensaje de login exitoso
            WeakReferenceMessenger.Default.Register<LoginSuccessMessage>(this, (r, m) =>
            {
                // Al recibir el mensaje de éxito, establecer AppShell como la MainPage de la aplicación
                Application.Current.MainPage = new AppShell();
            });

            // Try to start WorkerService and TrayApp when the MAUI app initializes (on Windows only)
#if WINDOWS
            _workerServiceManager = _serviceProvider.GetService<IWorkerServiceManager>();
            if (_workerServiceManager != null)
            {
                Task.Run(async () =>
                {
                    await _workerServiceManager.StartWorkerServiceAsync();
                });
            }

            _trayAppService = _serviceProvider.GetService<ITrayAppService>();
            if (_trayAppService != null)
            {
                Task.Run(async () =>
                {
                    await _trayAppService.StartTrayAppAsync();
                });
            }
#endif
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            // La ventana inicial contendrá la LoginView.
            // Una vez que el login sea exitoso, la MainPage de la aplicación se reemplazará por AppShell.
            var window = new Window(new LoginView());
            return window;
        }
    }

    // Definir un mensaje simple para comunicar el éxito del login
    public class LoginSuccessMessage { }
}