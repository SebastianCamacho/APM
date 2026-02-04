using AppsielPrintManager.Core.Interfaces;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text.Json;
using System.Text;

namespace UI.Platforms.Windows.Services
{
    public class WindowsPlatformService : IPlatformService
    {
        private readonly IWorkerServiceManager _workerServiceManager;
        private readonly ILoggingService _logger;

        private int _cachedClientCount;
        private bool _cachedIsRunning;
        private readonly HttpClient _httpClient;

        public WindowsPlatformService(IWorkerServiceManager workerServiceManager, ILoggingService logger)
        {
            _workerServiceManager = workerServiceManager;
            _logger = logger;
            _httpClient = new HttpClient { Timeout = System.TimeSpan.FromSeconds(0.5) };

            // Start background polling
            Task.Run(MonitorWorkerStatus);
        }

        private async Task MonitorWorkerStatus()
        {
            while (true)
            {
                try
                {
                    if (_workerServiceManager.IsWorkerServiceRunning)
                    {
                        // Endpoint added to WebSocketServerService: http://localhost:7000/websocket/status
                        var response = await _httpClient.GetStringAsync("http://localhost:7000/websocket/status");
                        if (!string.IsNullOrEmpty(response))
                        {
                            var doc = JsonDocument.Parse(response);
                            if (doc.RootElement.TryGetProperty("ConnectedClients", out var clientsProp))
                            {
                                _cachedClientCount = clientsProp.GetInt32();
                            }
                            // We assume if we got a response, it's running.
                            _cachedIsRunning = true;
                        }
                    }
                    else
                    {
                        _cachedClientCount = 0;
                        _cachedIsRunning = false;
                    }
                }
                catch
                {
                    // Service likely starting or not responding yet
                    _cachedIsRunning = false;
                    _cachedClientCount = 0;
                }

                await Task.Delay(1000); // Update every 1 second as requested
            }
        }

        public bool IsBackgroundServiceRunning => _workerServiceManager.IsWorkerServiceRunning;

        // On Windows, the WorkerService.exe acts as the WebSocket Host.
        // Therefore, if the worker is running, we assume the server is running (or starting).
        // Accurate client counting would require IPC, but for now we link status to the process.
        public bool IsWebSocketServerRunning => _cachedIsRunning;

        public int CurrentClientCount => _cachedClientCount;

        public async Task StartBackgroundServiceAsync()
        {
            _logger.LogInfo("[WindowsPlatformService] Requesting Worker Service start...");
            await _workerServiceManager.StartWorkerServiceAsync();
        }

        public async Task StopBackgroundServiceAsync()
        {
            _logger.LogInfo("[WindowsPlatformService] Requesting Worker Service stop...");
            await _workerServiceManager.StopWorkerServiceAsync();
        }

        public void ShowNotification(string title, string message)
        {
            // Windows Notification logic could go here if needed, or via TrayIcon
            _logger.LogInfo($"[WindowsPlatformService] Notification: {title} - {message}");
        }
    }
}
