using AppsielPrintManager.Core.Interfaces;
using AppsielPrintManager.Infraestructure.Repositories;
using AppsielPrintManager.Infraestructure.Services;
using CommunityToolkit.Maui;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UI.Converters;
using UI.Services;
using UI.ViewModels;
using UI.Views;

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

            builder.Services.AddSingleton<ILoggingService, Logger>();
            builder.Services.AddSingleton<ISettingsRepository, SettingsRepository>();
            builder.Services.AddSingleton<ITemplateRepository, TemplateRepository>();
            builder.Services.AddSingleton<ITicketRenderer, TicketRendererService>();
            builder.Services.AddSingleton<IEscPosGenerator, EscPosGeneratorService>();
            builder.Services.AddSingleton<TcpIpPrinterClient>();
            builder.Services.AddSingleton<IPrintService, PrintService>();

            // Register Scale Repository (Shared with Worker but separate instance/file access)
            builder.Services.AddSingleton<IScaleRepository, JsonScaleRepository>();

#if ANDROID
            builder.Services.AddSingleton<IPlatformService, UI.Platforms.Android.Services.AndroidPlatformService>();
            builder.Services.AddSingleton<IWebSocketService, AndroidWebSocketService>();
#elif WINDOWS
            builder.Services.AddSingleton<IWorkerServiceManager, WindowsWorkerServiceManager>();
            builder.Services.AddSingleton<ITrayAppService, WindowsTrayAppService>();
            builder.Services.AddSingleton<IPlatformService, UI.Platforms.Windows.Services.WindowsPlatformService>();
            
            // Register ScaleService for dependency resolution (used by WebSocketServerService)
            builder.Services.AddSingleton<IScaleService, SerialScaleService>();
            
            // On Windows, assuming UI acts as Controller, but keep WS service available if needed
            builder.Services.AddSingleton<IWebSocketService, WebSocketServerService>();
#else
            builder.Services.AddSingleton<IPlatformService, StubPlatformService>();
            builder.Services.AddSingleton<IWebSocketService, WebSocketServerService>();
#endif

            builder.Services.AddTransient<HomeViewModel>();
            builder.Services.AddTransient<PrintersViewModel>();
            builder.Services.AddTransient<PrinterDetailViewModel>();
            builder.Services.AddTransient<ScalesViewModel>(); // Added
            builder.Services.AddTransient<ScaleDetailViewModel>(); // Added
            builder.Services.AddTransient<LogsViewModel>();
            builder.Services.AddTransient<SettingsViewModel>();
            builder.Services.AddTransient<TemplateEditorViewModel>();

            builder.Services.AddTransient<LoginView>();
            builder.Services.AddSingleton<AppShell>();
            builder.Services.AddTransient<HomePage>();
            builder.Services.AddTransient<PrintersPage>();
            builder.Services.AddTransient<ScalesPage>();
            builder.Services.AddTransient<ScaleDetailPage>(); // Added
            builder.Services.AddTransient<SettingsPage>();
            builder.Services.AddTransient<TemplateEditorPage>();

            builder.Services.AddSingleton<InverseBoolConverter>();
            builder.Services.AddSingleton<NumericToStringConverter>();
            builder.Services.AddSingleton<LogLevelToColorConverter>();

            var app = builder.Build();
            Services = app.Services;
            return app;
        }

        public static IServiceProvider Services { get; private set; }
    }
}
