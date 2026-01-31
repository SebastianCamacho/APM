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

        public App(IServiceProvider serviceProvider)
        {
            InitializeComponent();

            _serviceProvider = serviceProvider;
            UserAppTheme = AppTheme.Light;

            // Establecer LoginView como MainPage inicial
            MainPage = new LoginView();

            // Suscribirse al mensaje de login exitoso
            WeakReferenceMessenger.Default.Register<LoginSuccessMessage>(this, (r, m) =>
            {
                try
                {
                    var appShell = _serviceProvider.GetService<AppShell>();
                    if (appShell != null)
                    {
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            if (Application.Current?.MainPage is LoginView)
                            {
                                MainPage = appShell;
                            }
                        });
                    }
                }
                catch (ObjectDisposedException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"App cerrándose: {ex.Message}");
                }
            });

#if WINDOWS
            // En Windows, usamos IPlatformService para iniciar el Worker, ya que ahora 
            // IPlatformService (WindowsPlatformService) encapsula la lógica correcta.
            _platformService = _serviceProvider.GetService<IPlatformService>();
            if (_platformService != null)
            {
                Task.Run(async () =>
                {
                    try
                    {
                        await _platformService.StartBackgroundServiceAsync();
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
        }

        protected override void OnSleep()
        {
            base.OnSleep();
        }

        protected override void OnResume()
        {
            base.OnResume();
        }
    }

    public class LoginSuccessMessage { }
}