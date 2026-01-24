using AppsielPrintManager.Core.Interfaces;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json; // Para Pretty Print JSON

namespace ConsoleApp
{
    /// <summary>
    /// Servicio alojado para la aplicación de consola que gestiona el ciclo de vida
    /// del servidor WebSocket y la interacción con el usuario a través de la consola.
    /// </summary>
    public class ConsoleHostedService : IHostedService, IDisposable
    {
        private readonly IWebSocketService _webSocketService;
        private readonly ILoggingService _logger;
        private readonly IHostApplicationLifetime _appLifetime;
        private CancellationTokenSource _consoleReadCts;
        private Task _consoleReadTask;

        private const int WebSocketPort = 7000;

        /// <summary>
        /// Constructor del servicio alojado de consola.
        /// </summary>
        /// <param name="webSocketService">Servicio WebSocket inyectado.</param>
        /// <param name="logger">Servicio de logging inyectado.</param>
        /// <param name="appLifetime">Servicio de gestión del ciclo de vida de la aplicación host.</param>
        public ConsoleHostedService(IWebSocketService webSocketService, ILoggingService logger, IHostApplicationLifetime appLifetime)
        {
            _webSocketService = webSocketService;
            _logger = logger;
            _appLifetime = appLifetime;

            // Suscribirse a los eventos del WebSocketService
            _webSocketService.OnClientConnected += (sender, clientId) =>
            {
                _logger.LogInfo($"[WebSocket] Cliente conectado: {clientId}");
                return Task.CompletedTask;
            };
            _webSocketService.OnClientDisconnected += (sender, clientId) =>
            {
                _logger.LogInfo($"[WebSocket] Cliente desconectado: {clientId}");
                return Task.CompletedTask;
            };
            _webSocketService.OnPrintJobReceived += (sender, request) =>
            {
                _logger.LogInfo($"[WebSocket] PrintJobRequest recibido para JobId: {request.Message.JobId}. Delegando procesamiento.");
                return Task.CompletedTask;
            };
        }

        /// <summary>
        /// Se llama cuando el host inicia el servicio. Aquí se inicia el servidor WebSocket
        /// y se configura la lectura de comandos de consola.
        /// </summary>
        /// <param name="cancellationToken">Token de cancelación para la operación de inicio.</param>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInfo("Servicio de Consola Iniciado.");
            _logger.LogInfo("Presione 'S' para iniciar el servidor WebSocket, 'D' para detenerlo, 'E' para salir de la aplicación.");

            _consoleReadCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _consoleReadTask = Task.Run(() => ProcessConsoleCommands(_consoleReadCts.Token), _consoleReadCts.Token);

            return Task.CompletedTask;
        }

        /// <summary>
        /// Se llama cuando el host detiene el servicio. Aquí se detiene el servidor WebSocket
        /// y se liberan los recursos.
        /// </summary>
        /// <param name="cancellationToken">Token de cancelación para la operación de detención.</param>
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInfo("Servicio de Consola Detenido. Iniciando limpieza...");

            if (_consoleReadCts != null && !_consoleReadCts.IsCancellationRequested)
            {
                _consoleReadCts.Cancel();
                try
                {
                    await _consoleReadTask; // Esperar a que la tarea de lectura de consola termine.
                }
                catch (OperationCanceledException) { /* Ignorar */ }
            }

            if (_webSocketService.IsRunning)
            {
                await _webSocketService.StopServerAsync();
            }

            _logger.LogInfo("Servicio de Consola y WebSocket detenidos y recursos liberados.");
        }

        /// <summary>
        /// Procesa los comandos de usuario ingresados por consola.
        /// </summary>
        /// <param name="cancellationToken">Token de cancelación para detener el procesamiento de comandos.</param>
        private async Task ProcessConsoleCommands(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                // Esperar un poco para no consumir CPU innecesariamente en el bucle
                await Task.Delay(100, cancellationToken); 

                // Leer una tecla solo si hay alguna disponible para evitar bloqueo.
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(intercept: true).Key;
                    switch (key)
                    {
                        case ConsoleKey.S:
                            if (!_webSocketService.IsRunning)
                            {
                                await _webSocketService.StartServerAsync(WebSocketPort);
                                _logger.LogInfo($"Servidor WebSocket {( _webSocketService.IsRunning ? "iniciado" : "falló al iniciar")} en puerto {WebSocketPort}.");
                            }
                            else
                            {
                                _logger.LogWarning("El servidor WebSocket ya está en ejecución.");
                            }
                            break;
                        case ConsoleKey.D:
                            if (_webSocketService.IsRunning)
                            {
                                await _webSocketService.StopServerAsync();
                                _logger.LogInfo("Servidor WebSocket detenido.");
                            }
                            else
                            {
                                _logger.LogWarning("El servidor WebSocket no está en ejecución.");
                            }
                            break;
                        case ConsoleKey.E:
                            _logger.LogInfo("Solicitud de salida recibida. Deteniendo la aplicación...");
                            _appLifetime.StopApplication(); // Detener el host principal.
                            break;
                        default:
                            break;
                    }
                }
            }
            _logger.LogInfo("Procesamiento de comandos de consola detenido.");
        }

        public void Dispose()
        {
            _consoleReadCts?.Dispose();
        }
    }
}
