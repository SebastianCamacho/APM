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
            builder.Services.AddSingleton<IPlatformService, StubPlatformService>(); // Registra StubPlatformService
            //builder.Services.AddSingleton<IWebSocketService, WebSocketServerService>(); // REMOVED: Duplicate registration

            // Register WorkerServiceManager conditionally for Windows
#if WINDOWS
            builder.Services.AddSingleton<IWorkerServiceManager, WindowsWorkerServiceManager>();
#else
            // For other platforms, register a no-op or throw an exception if WorkerServiceManager is attempted to be used.
            builder.Services.AddSingleton<IWorkerServiceManager, NoOpWorkerServiceManager>();
#endif


            // ViewModels
            builder.Services.AddTransient<PrintersViewModel>();
            builder.Services.AddTransient<PrinterDetailViewModel>(); // Nuevo registro
            builder.Services.AddTransient<LogsViewModel>(); // Nuevo registro

            // Converters
            builder.Services.AddSingleton<InverseBoolConverter>(); // Nuevo registro
            builder.Services.AddSingleton<NumericToStringConverter>(); // Nuevo registro
            builder.Services.AddSingleton<LogLevelToColorConverter>(); // Nuevo registro

            return builder.Build();
        }
    }
    // No-op implementation for IWorkerServiceManager on non-Windows platforms
    // This prevents compilation errors on other platforms where WindowsWorkerServiceManager is not available
    public class NoOpWorkerServiceManager : IWorkerServiceManager
    {
        public bool IsWorkerServiceRunning => false;
        public Task<bool> StartWorkerServiceAsync() => Task.FromResult(false);
        public Task<bool> StopWorkerServiceAsync() => Task.FromResult(false);
    }
}
