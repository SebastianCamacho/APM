using AppsielPrintManager.Core.Interfaces;
using AppsielPrintManager.Core.Models; // Required for LogMessage

namespace WorkerService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IWebSocketService _webSocketService;
        private readonly ITemplateRepository _templateRepository; // Inyectar Repositorio
        private const int WebSocketPort = 7000;

        public Worker(ILogger<Worker> logger, IWebSocketService webSocketService, ITemplateRepository templateRepository)
        {
            _logger = logger;
            _webSocketService = webSocketService;
            _templateRepository = templateRepository;

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
            _webSocketService.OnPrintJobReceived += async (sender, args) =>
            {
                var request = args.Message;
                var clientId = args.ClientId;
                _logger.LogInformation($"[WebSocket] PrintJobRequest recibido de {clientId} para JobId: {request.JobId} en WorkerService.");
                // The PrintService is already processing this via WebSocketServerService's ProcessMessagesAsync.
            };
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("WorkerService iniciado. Intentando iniciar el servidor WebSocket.");

            try
            {
                // Validación de Plantillas al inicio
                await _templateRepository.EnsureDefaultTemplatesAsync();
                _logger.LogInformation("Plantillas predeterminadas validadas/creadas exitosamente.");

                await _webSocketService.StartServerAsync(WebSocketPort);
                _logger.LogInformation($"Servidor WebSocket {(_webSocketService.IsRunning ? "iniciado" : "falló al iniciar")} en puerto {WebSocketPort}.");
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

