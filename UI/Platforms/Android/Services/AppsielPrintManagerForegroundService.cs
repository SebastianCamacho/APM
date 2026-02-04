using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using AppsielPrintManager.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging; // Using the MAUI's ILogger for now
using System.Threading;
using System.Threading.Tasks;
using AppsielPrintManager.Core.Models;
using Microsoft.Maui.ApplicationModel; // For Platform.Current

namespace UI.Platforms.Android.Services
{
    [Service(ForegroundServiceType = global::Android.Content.PM.ForegroundService.TypeConnectedDevice)]
    public class AppsielPrintManagerForegroundService : Service
    {
        private const int SERVICE_NOTIFICATION_ID = 10001;
        private const string NOTIFICATION_CHANNEL_ID = "AppsielPrintManagerChannel";
        private const string NOTIFICATION_CHANNEL_NAME = "Appsiel Print Manager Notifications";
        private const int WEBSOCKET_PORT = 7000; // Puerto para el servidor WebSocket

        private ILoggingService? _logger;
        private IWebSocketService? _webSocketService;
        private IPrintService? _printService;
        private CancellationTokenSource? _cts;

        // Campos estáticos para compartir estado con AndroidPlatformService
        public static bool IsWebSocketRunning { get; private set; } = false;
        public static int ConnectedClients { get; private set; } = 0;

        public override void OnCreate()
        {
            base.OnCreate();

            // Resolver servicios desde el contenedor de DI
            // Usar IPlatformApplication.Current.Services para evitar advertencias y posibles problemas de ciclo de vida
            var services = IPlatformApplication.Current?.Services;
            if (services != null)
            {
                _logger = services.GetService<ILoggingService>();
                _webSocketService = services.GetService<IWebSocketService>();
                _printService = services.GetService<IPrintService>();
                _logger?.LogInfo("Foreground Service: OnCreate called. Services resolved via IPlatformApplication.");
            }
            else
            {
                // Fallback por si IPlatformApplication.Current es null (raro en MAUI app iniciada)
                _logger = global::Microsoft.Maui.MauiApplication.Current.Services.GetService<ILoggingService>();
                _webSocketService = global::Microsoft.Maui.MauiApplication.Current.Services.GetService<IWebSocketService>();
                _printService = global::Microsoft.Maui.MauiApplication.Current.Services.GetService<IPrintService>();
                _logger?.LogInfo("Foreground Service: OnCreate called. Services resolved via MauiApplication (Fallback).");
            }
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

            // Iniciar WebSocket Server automáticamente
            Task.Run(async () =>
            {
                try
                {
                    if (_webSocketService != null)
                    {
                        // Suscribir a eventos del WebSocket
                        _webSocketService.OnClientConnected += WebSocketService_OnClientConnected;
                        _webSocketService.OnClientDisconnected += WebSocketService_OnClientDisconnected;
                        _webSocketService.OnPrintJobReceived += WebSocketService_OnPrintJobReceived;

                        // Iniciar servidor WebSocket
                        await _webSocketService.StartServerAsync(WEBSOCKET_PORT);
                        IsWebSocketRunning = true;
                        _logger?.LogInfo($"Foreground Service: WebSocket Server iniciado en puerto {WEBSOCKET_PORT}");
                    }
                    else
                    {
                        _logger?.LogError("Foreground Service: IWebSocketService no pudo ser resuelto.");
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError($"Foreground Service: Error al iniciar WebSocket Server: {ex.Message}", ex);
                }
            });

            return StartCommandResult.Sticky;
        }

        public override IBinder OnBind(Intent intent)
        {
            _logger?.LogInfo("Foreground Service: OnBind called.");
            return null; // Not providing a bound service for now
        }

        public override void OnDestroy()
        {
            _logger?.LogInfo("Foreground Service: OnDestroy called.");

            // Detener WebSocket Server si está ejecutándose
            if (_webSocketService != null && IsWebSocketRunning)
            {
                try
                {
                    // Desuscribir eventos para evitar fugas de memoria
                    _webSocketService.OnClientConnected -= WebSocketService_OnClientConnected;
                    _webSocketService.OnClientDisconnected -= WebSocketService_OnClientDisconnected;
                    _webSocketService.OnPrintJobReceived -= WebSocketService_OnPrintJobReceived;

                    _webSocketService.StopServerAsync().Wait();
                    IsWebSocketRunning = false;
                    ConnectedClients = 0;
                    _logger?.LogInfo("Foreground Service: WebSocket Server detenido.");
                }
                catch (Exception ex)
                {
                    _logger?.LogError($"Foreground Service: Error al detener WebSocket Server: {ex.Message}", ex);
                }
            }

            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;

            StopForeground(true);
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

        // --- Event Handlers para IWebSocketService ---

        /// <summary>
        /// Manejador del evento OnClientConnected del WebSocket.
        /// </summary>
        private Task WebSocketService_OnClientConnected(object sender, string clientId)
        {
            ConnectedClients++;
            _logger?.LogInfo($"[WebSocket] Cliente conectado: {clientId}. Total clientes: {ConnectedClients}");
            return Task.CompletedTask;
        }

        /// <summary>
        /// Manejador del evento OnClientDisconnected del WebSocket.
        /// </summary>
        private Task WebSocketService_OnClientDisconnected(object sender, string clientId)
        {
            ConnectedClients--;
            if (ConnectedClients < 0) ConnectedClients = 0;
            _logger?.LogInfo($"[WebSocket] Cliente desconectado: {clientId}. Total clientes: {ConnectedClients}");
            return Task.CompletedTask;
        }

        /// <summary>
        /// Manejador del evento OnPrintJobReceived del WebSocket.
        /// Procesa la solicitud de impresión y envía el resultado de vuelta CLIENTE ESPECÍFICO.
        /// </summary>
        private async Task WebSocketService_OnPrintJobReceived(object sender, WebSocketMessageReceivedEventArgs<PrintJobRequest> e)
        {
            try
            {
                var request = e.Message;
                var clientId = e.ClientId;
                _logger?.LogInfo($"[WebSocket] PrintJobRequest recibido de {clientId} para JobId: {request.JobId}. Procesando impresión.");

                if (_printService != null)
                {
                    // Procesar el trabajo de impresión
                    var printResult = await _printService.ProcessPrintJobAsync(request);
                    _logger?.LogInfo($"[WebSocket] PrintJob {request.JobId} procesado con estado: {printResult.Status}");

                    // Enviar resultado de vuelta SOLO al cliente que lo solicitó
                    if (_webSocketService != null)
                    {
                        // Usar el nuevo método Unicast
                        await _webSocketService.SendPrintJobResultToClientAsync(clientId, printResult);
                        _logger?.LogInfo($"[WebSocket] Resultado del PrintJob {request.JobId} enviado al cliente {clientId}.");
                    }
                }
                else
                {
                    _logger?.LogError("[WebSocket] IPrintService no está disponible para procesar el PrintJobRequest.");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError($"[WebSocket] Error al procesar PrintJobRequest: {ex.Message}", ex);
            }
        }
    }
}