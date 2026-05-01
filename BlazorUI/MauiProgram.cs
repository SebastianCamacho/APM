using AppsielPrintManager.Core.Interfaces;
using AppsielPrintManager.Infraestructure.Repositories;
using AppsielPrintManager.Infraestructure.Services;
using Microsoft.Extensions.Logging;
using BlazorUI.Services;

namespace BlazorUI
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            builder.Services.AddMauiBlazorWebView();

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

            // Servicios Base
            builder.Services.AddSingleton<ILoggingService, Logger>();
            builder.Services.AddSingleton<ILoggerProvider>(sp => new AppsielLoggerProvider(sp.GetRequiredService<ILoggingService>()));
            builder.Services.AddSingleton<AuthState>();
            builder.Services.AddSingleton<IDialogService, MauiDialogService>(); // Servicio de diálogos desacoplado de MAUI
            builder.Services.AddSingleton<ISettingsRepository, SettingsRepository>();
            builder.Services.AddSingleton<IAppConfigRepository, AppConfigRepository>();
            builder.Services.AddSingleton<ITemplateRepository, TemplateRepository>();
            builder.Services.AddSingleton<ITicketRenderer, TicketRendererService>();
            builder.Services.AddSingleton<IEscPosGenerator, EscPosGeneratorService>();
            builder.Services.AddSingleton<TcpIpPrinterClient>();

            // Servicios para Impresoras Matriciales
            builder.Services.AddSingleton<DotMatrixRendererService>();
            builder.Services.AddSingleton<EscPGeneratorService>();
            builder.Services.AddSingleton<LocalRawPrinterClient>();
            builder.Services.AddSingleton<IppPrinterClient>();

            builder.Services.AddSingleton<IPrintService, PrintService>();

            // Register Scale Repository
            builder.Services.AddSingleton<IScaleRepository, JsonScaleRepository>();

#if ANDROID
            builder.Services.AddSingleton<IPlatformService, BlazorUI.Platforms.Android.Services.AndroidPlatformService>();
            builder.Services.AddSingleton<IWebSocketService, AndroidWebSocketService>();
#elif WINDOWS
            builder.Services.AddSingleton<IWorkerServiceManager, WindowsWorkerServiceManager>();
            builder.Services.AddSingleton<ITrayAppService, WindowsTrayAppService>();
            builder.Services.AddSingleton<IPlatformService, BlazorUI.Platforms.Windows.Services.WindowsPlatformService>();
            
            // Register ScaleService for dependency resolution (used by WebSocketServerService)
            builder.Services.AddSingleton<IScaleService, SerialScaleService>();
            
            // On Windows keep WS service available if needed
            builder.Services.AddSingleton<IWebSocketService, WebSocketServerService>();
#else
            builder.Services.AddSingleton<IPlatformService, StubPlatformService>();
            builder.Services.AddSingleton<IWebSocketService, WebSocketServerService>();
#endif

            var app = builder.Build();
            Services = app.Services;
            return app;
        }

        public static IServiceProvider Services { get; private set; }
    }
}
