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
            builder.Services.AddSingleton<IWebSocketService, WebSocketServerService>();
            builder.Services.AddSingleton<ISettingsRepository, SettingsRepository>();
            builder.Services.AddSingleton<ITicketRenderer, TicketRendererService>();
            builder.Services.AddSingleton<IEscPosGenerator, EscPosGeneratorService>();
            builder.Services.AddSingleton<TcpIpPrinterClient>();
            builder.Services.AddSingleton<IPrintService, PrintService>();
#if ANDROID
            builder.Services.AddSingleton<IPlatformService, UI.Platforms.Android.Services.AndroidPlatformService>();
#else
            builder.Services.AddSingleton<IPlatformService, StubPlatformService>();
#endif

#if WINDOWS
            builder.Services.AddSingleton<IWorkerServiceManager, WindowsWorkerServiceManager>();
            builder.Services.AddSingleton<ITrayAppService, WindowsTrayAppService>();
#endif

            builder.Services.AddTransient<HomeViewModel>();
            builder.Services.AddTransient<PrintersViewModel>();
            builder.Services.AddTransient<PrinterDetailViewModel>();
            builder.Services.AddTransient<LogsViewModel>();

            builder.Services.AddTransient<LoginView>();
            builder.Services.AddSingleton<AppShell>();
            builder.Services.AddTransient<HomePage>();
            builder.Services.AddTransient<PrintersPage>();
            builder.Services.AddTransient<ScalesPage>();

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
