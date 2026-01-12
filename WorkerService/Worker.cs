using AppsielPrintManager.Core.Interfaces;
using AppsielPrintManager.Core.Models; // Required for LogMessage

namespace WorkerService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IWebSocketService _webSocketService;
        private const int WebSocketPort = 7000;

        public Worker(ILogger<Worker> logger, IWebSocketService webSocketService)
        {
            _logger = logger;
            _webSocketService = webSocketService;

            // Subscribe to WebSocketService events
            _webSocketService.OnClientConnected += (sender, clientId) =>
            {
                _logger.LogInformation($"[WebSocket] Cliente conectado: {clientId}");
                return Task.CompletedTask;
            };
            _webSocketService.OnClientDisconnected += (sender, clientId) =>
            {
                _logger.LogInformation($"[WebSocket] Cliente desconectado: {clientId}");
                return Task.CompletedTask;
            };
            _webSocketService.OnPrintJobReceived += async (sender, request) =>
            {
                _logger.LogInformation($"[WebSocket] PrintJobRequest recibido para JobId: {request.JobId} en WorkerService.");
                // The PrintService is already processing this via WebSocketServerService's ProcessMessagesAsync,
                // so we just log here. If WorkerService were to directly process, this is where we'd trigger it.
            };
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("WorkerService iniciado. Intentando iniciar el servidor WebSocket.");

            try
            {
                await _webSocketService.StartServerAsync(WebSocketPort);
                _logger.LogInformation($"Servidor WebSocket {( _webSocketService.IsRunning ? "iniciado" : "falló al iniciar")} en puerto {WebSocketPort}.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al iniciar el servidor WebSocket en WorkerService: {ex.Message}", ex);
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                // WorkerService's main task is to keep the WebSocket server running.
                // We can add other background tasks here if needed.
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); // Log less frequently
            }

            _logger.LogInformation("WorkerService detenido. Deteniendo el servidor WebSocket.");
            if (_webSocketService.IsRunning)
            {
                await _webSocketService.StopServerAsync();
            }
        }
    }
}

