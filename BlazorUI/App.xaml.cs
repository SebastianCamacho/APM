using AppsielPrintManager.Core.Interfaces;

namespace BlazorUI
{
    public partial class App : Application
    {
        private readonly IServiceProvider _serviceProvider;

        public App(IServiceProvider serviceProvider)
        {
            InitializeComponent();
            _serviceProvider = serviceProvider;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new MainPage()) { Title = "Appsiel Print Manager" };
        }

        protected override void OnStart()
        {
            base.OnStart();

            // Fire and forget para no bloquear el hilo de UI
            Task.Run(async () =>
            {
                try
                {
                    // Dar un pequeño delay de gracia para que la UI termine de montarse 
                    // y los prompt de permisos de Android tengan una vista atada.
                    await Task.Delay(1500);

                    using var scope = _serviceProvider.CreateScope();
                    
#if WINDOWS
                    var platformService = scope.ServiceProvider.GetService<IPlatformService>();
                    if (platformService != null && !platformService.IsBackgroundServiceRunning)
                    {
                        await platformService.StartBackgroundServiceAsync();
                    }

                    var trayAppService = scope.ServiceProvider.GetService<ITrayAppService>();
                    if (trayAppService != null)
                    {
                        await trayAppService.StartTrayAppAsync();
                    }
#elif ANDROID
                    var platformService = scope.ServiceProvider.GetService<IPlatformService>();
                    if (platformService != null && !platformService.IsBackgroundServiceRunning)
                    {
                        await platformService.StartBackgroundServiceAsync();
                    }
#endif
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error iniciando servicios de fondo fijos: {ex.Message}");
                }
            });
        }
    }
}
