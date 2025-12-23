using Microsoft.Extensions.Logging;
using AppsielPrintManager.Core.Interfaces;
using AppsielPrintManager.Infraestructure.Services;
using AppsielPrintManager.Infraestructure.Repositories; // Para ISettingsRepository, SettingsRepository

#if ANDROID
using UI.Platforms.Android.Services; // Para AndroidPlatformService
#elif WINDOWS
using UI.Platforms.Windows.Services; // Para WindowsPlatformService
#endif


namespace UI
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
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
    		builder.Logging.AddDebug();
#endif
            // Registro de servicios de Core e Infraestructure
            builder.Services.AddSingleton<ILoggingService, Logger>();
            builder.Services.AddSingleton<IWebSocketService, WebSocketServerService>();
            builder.Services.AddSingleton<ISettingsRepository, SettingsRepository>(); // Registrar el repositorio de configuraciones

            // Registro de servicios específicos de la plataforma
#if ANDROID
            builder.Services.AddSingleton<IPlatformService, UI.Platforms.Android.Services.AndroidPlatformService>();
#elif WINDOWS
            builder.Services.AddSingleton<IPlatformService, UI.Platforms.Windows.Services.WindowsPlatformService>();
#endif

            return builder.Build();
        }
    }
}
