using AppsielPrintManager.Core.Interfaces;
using AppsielPrintManager.Infraestructure.Repositories;
using AppsielPrintManager.Infraestructure.Services;
using CommunityToolkit.Maui;
using Microsoft.Extensions.DependencyInjection; // Added for service registration
using Microsoft.Extensions.Logging;
using UI.Converters; // Añadir esta línea
using UI.Services;
using UI.ViewModels;

namespace UI
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
    		builder.Logging.AddDebug();
#endif
            // Register Core and Infraestructure services
            builder.Services.AddSingleton<ILoggingService, Logger>();
            builder.Services.AddSingleton<IWebSocketService, WebSocketServerService>();
            builder.Services.AddSingleton<ISettingsRepository, SettingsRepository>();
            builder.Services.AddSingleton<ITicketRenderer, TicketRendererService>(); // Nuevo registro
            builder.Services.AddSingleton<IEscPosGenerator, EscPosGeneratorService>(); // Nuevo registro
            builder.Services.AddSingleton<TcpIpPrinterClient>(); // Nuevo registro
            builder.Services.AddSingleton<IPrintService, PrintService>(); // Nuevo registro
#if ANDROID
            builder.Services.AddSingleton<IPlatformService, UI.Platforms.Android.Services.AndroidPlatformService>(); // Registra AndroidPlatformService
#else
            builder.Services.AddSingleton<IPlatformService, StubPlatformService>(); // Registra StubPlatformService (para otras plataformas, incluyendo iOS)
#endif
            //builder.Services.AddSingleton<IWebSocketService, WebSocketServerService>(); // REMOVED: Duplicate registration

            // Register WorkerServiceManager conditionally for Windows
#if WINDOWS
            builder.Services.AddSingleton<IWorkerServiceManager, WindowsWorkerServiceManager>();
            builder.Services.AddSingleton<ITrayAppService, WindowsTrayAppService>(); // Nuevo registro para TrayApp
#else
            // Para otras plataformas, se utilizará la implementación de IPlatformService o se gestionará la ausencia del servicio.
            // No se registran NoOpWorkerServiceManager ni NoOpTrayAppService ya que IPlatformService gestionará la interacción.
#endif


            // ViewModels
            builder.Services.AddTransient<HomeViewModel>(); // Nuevo registro para HomePage
            builder.Services.AddTransient<PrintersViewModel>();
            builder.Services.AddTransient<PrinterDetailViewModel>(); // Nuevo registro
            builder.Services.AddTransient<LogsViewModel>(); // Nuevo registro

            // Converters
            builder.Services.AddSingleton<InverseBoolConverter>(); // Nuevo registro
            builder.Services.AddSingleton<NumericToStringConverter>(); // Nuevo registro
            builder.Services.AddSingleton<LogLevelToColorConverter>(); // Nuevo registro

            var app = builder.Build();
            Services = app.Services; // Expose the service provider
            return app;
        }

        public static IServiceProvider Services { get; private set; } // Static property to hold the service provider
    }
}
