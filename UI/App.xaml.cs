using UI.Views;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Maui.Controls;
using AppsielPrintManager.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

namespace UI
{
    public partial class App : Application
    {
        private readonly IServiceProvider _serviceProvider;
        private IWorkerServiceManager _workerServiceManager;
        private ITrayAppService _trayAppService;
        private IPlatformService _platformService;
        private bool _isClosing = false;

        public App(IServiceProvider serviceProvider)
        {
            InitializeComponent();

            _serviceProvider = serviceProvider;
            UserAppTheme = AppTheme.Light;

            // Suscribirse al mensaje de login exitoso
            WeakReferenceMessenger.Default.Register<LoginSuccessMessage>(this, (r, m) =>
            {
                // Solo cambiar MainPage si la app no se está cerrando
                if (!_isClosing && Application.Current != null)
                {
                    try
                    {
                        var appShell = _serviceProvider.GetService<AppShell>();
                        if (appShell != null)
                        {
                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                Application.Current.MainPage = appShell;
                            });
                        }
                    }
                    catch (ObjectDisposedException)
                    {
                        System.Diagnostics.Debug.WriteLine("ServiceProvider disposado - login fallido");
                    }
                }
            });

#if WINDOWS
            _workerServiceManager = _serviceProvider.GetService<IWorkerServiceManager>();
            if (_workerServiceManager != null)
            {
                Task.Run(async () =>
                {
                    try
                    {
                        await _workerServiceManager.StartWorkerServiceAsync();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error starting worker service: {ex}");
                    }
                });
            }

            _trayAppService = _serviceProvider.GetService<ITrayAppService>();
            if (_trayAppService != null)
            {
                Task.Run(async () =>
                {
                    try
                    {
                        await _trayAppService.StartTrayAppAsync();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error starting tray app: {ex}");
                    }
                });
            }
#elif ANDROID
            _platformService = _serviceProvider.GetService<IPlatformService>();
            if (_platformService != null)
            {
                Task.Run(async () =>
                {
                    try
                    {
                        if (!_platformService.IsBackgroundServiceRunning)
                        {
                            await _platformService.StartBackgroundServiceAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error starting background service: {ex}");
                    }
                });
            }
#endif
        }

        protected override void OnStart()
        {
            base.OnStart();
            _isClosing = false;
        }

        protected override void OnSleep()
        {
            _isClosing = true;
            base.OnSleep();
        }

        protected override void OnResume()
        {
            _isClosing = false;
            base.OnResume();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = new Window(new LoginView());
            return window;
        }
    }

    public class LoginSuccessMessage { }
}