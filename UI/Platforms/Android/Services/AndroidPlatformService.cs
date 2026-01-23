using Android.App;
using Android.Content;
using Android.OS;
using AppsielPrintManager.Core.Interfaces;
using UI.Platforms.Android.Services;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel; // Added for MauiApplication.Current.ApplicationContext

namespace UI.Platforms.Android.Services
{
    public class AndroidPlatformService : IPlatformService
    {
        private readonly ILoggingService _logger;

        public AndroidPlatformService(ILoggingService logger)
        {
            _logger = logger;
        }

        public bool IsBackgroundServiceRunning
        {
            get
            {
                // Check if the foreground service is running
                ActivityManager manager = (ActivityManager)MauiApplication.Current.ApplicationContext.GetSystemService(Context.ActivityService);
                foreach (var service in manager.GetRunningServices(int.MaxValue))
                {
                    if (service.Service.ClassName.Equals(Java.Lang.Class.FromType(typeof(AppsielPrintManagerForegroundService)).CanonicalName))
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public Task StartBackgroundServiceAsync()
        {
            _logger.LogInfo("AndroidPlatformService: Requesting to start foreground service.");
            var intent = new Intent(MauiApplication.Current.ApplicationContext, typeof(AppsielPrintManagerForegroundService));
            // StartForegroundService is required for Android O (API 26) and above
            MauiApplication.Current.ApplicationContext.StartForegroundService(intent);
            return Task.CompletedTask;
        }

        public Task StopBackgroundServiceAsync()
        {
            _logger.LogInfo("AndroidPlatformService: Requesting to stop foreground service.");
            var intent = new Intent(MauiApplication.Current.ApplicationContext, typeof(AppsielPrintManagerForegroundService));
            MauiApplication.Current.ApplicationContext.StopService(intent);
            return Task.CompletedTask;
        }

        public void ShowNotification(string title, string message)
        {
            _logger.LogInfo($"AndroidPlatformService: Showing notification: {title} - {message}");
            // This method is already handled by the Foreground Service's own notification mechanism.
            // If we want to show additional, non-persistent notifications, we would implement it here.
            // For now, let's just log it.
        }

        public bool IsWebSocketServerRunning
        {
            get
            {
                // Consultar el estado del WebSocket desde el Foreground Service
                return AppsielPrintManagerForegroundService.IsWebSocketRunning;
            }
        }

        public int CurrentClientCount
        {
            get
            {
                // Consultar el número de clientes desde el Foreground Service
                return AppsielPrintManagerForegroundService.ConnectedClients;
            }
        }
    }
}