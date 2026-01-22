using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using AppsielPrintManager.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging; // Using the MAUI's ILogger for now
using System.Threading;
using System.Threading.Tasks;

namespace UI.Platforms.Android.Services
{
    [Service(ForegroundServiceType = global::Android.Content.PM.ForegroundService.TypeConnectedDevice)]
    public class AppsielPrintManagerForegroundService : Service
    {
        private const int SERVICE_NOTIFICATION_ID = 10001;
        private const string NOTIFICATION_CHANNEL_ID = "AppsielPrintManagerChannel";
        private const string NOTIFICATION_CHANNEL_NAME = "Appsiel Print Manager Notifications";

        private ILoggingService _logger; // Will be resolved later
        private CancellationTokenSource _cts;

        public override void OnCreate()
        {
            base.OnCreate();
            // TODO: Resolve ILoggingService safely here. This will be addressed in Phase 3.
            _logger = global::Microsoft.Maui.MauiApplication.Current.Services.GetService<ILoggingService>();
            _logger?.LogInfo("Foreground Service: OnCreate called.");
        }

        [return: GeneratedEnum]
        public override StartCommandResult OnStartCommand(Intent intent, [GeneratedEnum] StartCommandFlags flags, int startId)
        {
            _logger?.LogInfo("Foreground Service: OnStartCommand called.");

            _cts = new CancellationTokenSource();

            // Create a notification for the foreground service
            Notification notification = CreateNotification();

            // Start the service in the foreground
            StartForeground(SERVICE_NOTIFICATION_ID, notification);

            // TODO: Start WebSocketServerService and other long-running tasks here.
            // This will be implemented in Phase 4, after dependency resolution in Phase 3.
            Task.Run(async () =>
            {
                // Placeholder for actual work
                while (!_cts.Token.IsCancellationRequested)
                {
                    _logger?.LogInfo("Foreground Service: Working in background...");
                    await Task.Delay(5000, _cts.Token);
                }
            });

            return StartCommandResult.Sticky; // Service will be restarted if killed by the system
        }

        public override IBinder OnBind(Intent intent)
        {
            _logger?.LogInfo("Foreground Service: OnBind called.");
            return null; // Not providing a bound service for now
        }

        public override void OnDestroy()
        {
            _logger?.LogInfo("Foreground Service: OnDestroy called.");
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;

            StopForeground(true); // Remove notification
            base.OnDestroy();
        }

        private Notification CreateNotification()
        {
            // Ensure Notification Channel exists for Android O and above
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var channel = new NotificationChannel(NOTIFICATION_CHANNEL_ID, NOTIFICATION_CHANNEL_NAME, NotificationImportance.Low);
                var notificationManager = (NotificationManager)GetSystemService(NotificationService);
                notificationManager.CreateNotificationChannel(channel);
            }

            // Create an intent for the main activity to open when notification is tapped
            Intent notificationIntent = PackageManager.GetLaunchIntentForPackage(PackageName);
            PendingIntent pendingIntent = PendingIntent.GetActivity(this, 0, notificationIntent, PendingIntentFlags.Immutable);

            // Build the notification
            var notificationBuilder = new Notification.Builder(this, NOTIFICATION_CHANNEL_ID)
                .SetContentTitle("Appsiel Print Manager")
                .SetContentText("Servicio en segundo plano ejecutándose...")
                .SetSmallIcon(Resource.Drawable.notification_icon) // Usar el nuevo icono de notificación
                .SetContentIntent(pendingIntent)
                .SetOngoing(true) // Makes the notification persistent
                .SetAutoCancel(false); // Do not auto cancel when tapped

            return notificationBuilder.Build();
        }
    }
}