using AppsielPrintManager.Core.Interfaces;
using AppsielPrintManager.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using WatsonWebsocket;

namespace AppsielPrintManager.Infraestructure.Services
{
    /// <summary>
    /// Implementación del servicio WebSocket para Android usando WatsonWebsocket.
    /// Este servidor escucha conexiones WebSocket desde clientes (ej. páginas web de Appsiel),
    /// recibe solicitudes de impresión y envía resultados.
    /// NO implementa funcionalidades de báscula según diseño del proyecto.
    /// </summary>
    public class AndroidWebSocketService : IWebSocketService
    {
        private readonly ILoggingService _logger;
        private WatsonWsServer? _server;
        private int _currentClientCount = 0;
        private readonly object _lockObject = new object();

        /// <summary>
        /// Evento que se dispara cuando un cliente WebSocket se conecta al servidor APM.
        /// </summary>
        public event AsyncEventHandler<string>? OnClientConnected;

        /// <summary>
        /// Evento que se dispara cuando un cliente WebSocket se desconecta del servidor APM.
        /// </summary>
        public event AsyncEventHandler<string>? OnClientDisconnected;

        /// <summary>
        /// Evento que se dispara cuando se recibe una solicitud de trabajo de impresión desde un cliente conectado.
        /// </summary>
        public event AsyncEventHandler<WebSocketMessageReceivedEventArgs<PrintJobRequest>>? OnPrintJobReceived;

        /// <summary>
        /// Evento que se dispara cuando se recibe una solicitud de actualización de plantilla.
        /// </summary>
        public event AsyncEventHandler<WebSocketMessageReceivedEventArgs<PrintTemplate>>? OnTemplateUpdateReceived;

        /// <summary>
        /// Indica si el servidor WebSocket está actualmente escuchando conexiones.
        /// </summary>
        public bool IsRunning
        {
            get
            {
                lock (_lockObject)
                {
                    return _server != null;
                }
            }
        }

        /// <summary>
        /// Obtiene el número actual de clientes conectados al servidor WebSocket.
        /// </summary>
        public int CurrentClientCount
        {
            get
            {
                lock (_lockObject)
                {
                    return _currentClientCount;
                }
            }
        }

        public AndroidWebSocketService(ILoggingService logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Inicia el servidor WebSocket local en el puerto especificado.
        /// </summary>
        /// <param name="port">El número de puerto en el que el servidor escuchará.</param>
        /// <returns>Tarea que representa la operación asíncrona de inicio del servidor.</returns>
        public Task StartServerAsync(int port)
        {
            lock (_lockObject)
            {
                if (_server != null)
                {
                    _logger.LogWarning("AndroidWebSocketService: El servidor WebSocket ya está en ejecución.");
                    return Task.CompletedTask;
                }

                try
                {
                    // Crear instancia del servidor Watson WebSocket
                    // Escuchar en todas las interfaces ("+" o "*") para permitir conexiones externas
                    // Primer parámetro: IP (usar "+" para todas las interfaces)
                    // Segundo parámetro: Puerto
                    // Tercer parámetro: SSL habilitado (false para conexiones no seguras)
                    _server = new WatsonWsServer("+", port, false);

                    // Suscribir a los eventos del servidor
                    _server.ClientConnected += OnWatsonClientConnected;
                    _server.ClientDisconnected += OnWatsonClientDisconnected;
                    _server.MessageReceived += OnWatsonMessageReceived;

                    // Iniciar el servidor
                    _server.Start();

                    _logger.LogInfo($"AndroidWebSocketService: Servidor WebSocket iniciado en puerto {port}");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"AndroidWebSocketService: Error al iniciar el servidor WebSocket: {ex.Message}", ex);

                    // Limpiar en caso de error
                    if (_server != null)
                    {
                        try
                        {
                            _server.Stop();
                            _server.Dispose();
                        }
                        catch { }
                        _server = null!;
                    }

                    throw;
                }
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Detiene el servidor WebSocket local, cerrando todas las conexiones de clientes activas.
        /// </summary>
        /// <returns>Tarea que representa la operación asíncrona de detención del servidor.</returns>
        public Task StopServerAsync()
        {
            lock (_lockObject)
            {
                if (_server == null)
                {
                    _logger.LogWarning("AndroidWebSocketService: El servidor WebSocket no está en ejecución.");
                    return Task.CompletedTask;
                }

                try
                {
                    // Desuscribir de eventos para evitar fugas de memoria
                    _server.ClientConnected -= OnWatsonClientConnected;
                    _server.ClientDisconnected -= OnWatsonClientDisconnected;
                    _server.MessageReceived -= OnWatsonMessageReceived;

                    // Detener el servidor
                    _server.Stop();
                    _server.Dispose();

                    _logger.LogInfo("AndroidWebSocketService: Servidor WebSocket detenido.");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"AndroidWebSocketService: Error al detener el servidor WebSocket: {ex.Message}", ex);
                }
                finally
                {
                    _server = null!;
                    _currentClientCount = 0;
                }
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Envía un resultado de trabajo de impresión de vuelta a todos los clientes WebSocket conectados.
        /// </summary>
        /// <param name="result">El objeto PrintJobResult que contiene el estado del trabajo.</param>
        /// <returns>Tarea que representa la operación asíncrona de envío.</returns>
        public async Task SendPrintJobResultToAllClientsAsync(PrintJobResult result)
        {
            WatsonWsServer? server;
            lock (_lockObject)
            {
                server = _server;
            }

            if (server == null)
            {
                _logger.LogWarning("AndroidWebSocketService: No se puede enviar PrintJobResult, el servidor no está en ejecución.");
                return;
            }

            try
            {
                var json = JsonSerializer.Serialize(result);
                var clients = server.ListClients();

                if (clients == null || !clients.Any())
                {
                    _logger.LogWarning("AndroidWebSocketService: No hay clientes conectados para enviar PrintJobResult.");
                    return;
                }

                foreach (var client in clients)
                {
                    try
                    {
                        await server.SendAsync(client.Guid, json);
                        _logger.LogInfo($"AndroidWebSocketService: PrintJobResult enviado al cliente {client.Guid}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"AndroidWebSocketService: Error al enviar PrintJobResult al cliente {client.Guid}: {ex.Message}", ex);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"AndroidWebSocketService: Error al serializar o enviar PrintJobResult: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Envía un resultado de trabajo de impresión a un cliente específico.
        /// </summary>
        public async Task SendPrintJobResultToClientAsync(string clientId, PrintJobResult result)
        {
            WatsonWsServer? server;
            lock (_lockObject)
            {
                server = _server;
            }

            if (server == null)
            {
                _logger.LogWarning("AndroidWebSocketService: No se puede enviar PrintJobResult, el servidor no está en ejecución.");
                return;
            }

            try
            {
                // Convert string clientId to Guid
                if (!Guid.TryParse(clientId, out var clientGuid))
                {
                    _logger.LogError($"AndroidWebSocketService: Client ID inválido: {clientId}");
                    return;
                }

                var json = JsonSerializer.Serialize(result);
                await server.SendAsync(clientGuid, json);
                _logger.LogInfo($"AndroidWebSocketService: PrintJobResult enviado al cliente específico {clientId}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"AndroidWebSocketService: Error al enviar PrintJobResult al cliente {clientId}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Envía el resultado de una actualización de plantilla a un cliente específico.
        /// </summary>
        public async Task SendTemplateUpdateResultAsync(string clientId, TemplateUpdateResult result)
        {
            WatsonWsServer? server;
            lock (_lockObject)
            {
                server = _server;
            }

            if (server == null)
            {
                _logger.LogWarning("AndroidWebSocketService: No se puede enviar TemplateUpdateResult, el servidor no está en ejecución.");
                return;
            }

            try
            {
                if (!Guid.TryParse(clientId, out var clientGuid))
                {
                    _logger.LogError($"AndroidWebSocketService: Client ID inválido para TemplateUpdateResult: {clientId}");
                    return;
                }

                var json = JsonSerializer.Serialize(result);
                await server.SendAsync(clientGuid, json);
                _logger.LogInfo($"AndroidWebSocketService: TemplateUpdateResult enviado al cliente {clientId}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"AndroidWebSocketService: Error al enviar TemplateUpdateResult al cliente {clientId}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Envía datos de báscula a todos los clientes WebSocket conectados.
        /// NO IMPLEMENTADO para Android según especificaciones del proyecto.
        /// </summary>
        /// <param name="scaleData">El objeto ScaleData que contiene la lectura de la báscula.</param>
        /// <returns>Tarea que representa la operación asíncrona de envío.</returns>
        public Task SendScaleDataToAllClientsAsync(ScaleData scaleData)
        {
            _logger.LogWarning("AndroidWebSocketService: SendScaleDataToAllClientsAsync llamado pero no está implementado para Android.");
            throw new NotImplementedException("Scale data not applicable for Android.");
        }

        // --- Event Handlers para WatsonWebsocket ---

        /// <summary>
        /// Manejador del evento ClientConnected de Watson WebSocket.
        /// </summary>
        private void OnWatsonClientConnected(object? sender, ConnectionEventArgs args)
        {
            lock (_lockObject)
            {
                _currentClientCount++;
            }

            var clientId = args.Client.Guid.ToString();
            _logger.LogInfo($"AndroidWebSocketService: Cliente conectado: {clientId} desde {args.Client.IpPort}");

            // Disparar evento de la interfaz IWebSocketService
            OnClientConnected?.Invoke(this, clientId);
        }

        /// <summary>
        /// Manejador del evento ClientDisconnected de Watson WebSocket.
        /// </summary>
        private void OnWatsonClientDisconnected(object? sender, DisconnectionEventArgs args)
        {
            lock (_lockObject)
            {
                _currentClientCount--;
                if (_currentClientCount < 0) _currentClientCount = 0;
            }

            var clientId = args.Client.Guid.ToString();
            _logger.LogInfo($"AndroidWebSocketService: Cliente desconectado: {clientId}");

            // Disparar evento de la interfaz IWebSocketService
            OnClientDisconnected?.Invoke(this, clientId);
        }

        /// <summary>
        /// Manejador del evento MessageReceived de Watson WebSocket.
        /// Deserializa mensajes JSON a PrintJobRequest y dispara el evento correspondiente.
        /// </summary>
        private void OnWatsonMessageReceived(object? sender, MessageReceivedEventArgs args)
        {
            try
            {
                var clientId = args.Client.Guid.ToString();
                var message = Encoding.UTF8.GetString(args.Data.ToArray());

                _logger.LogInfo($"AndroidWebSocketService: Mensaje recibido de {clientId}: {message}");

                // Intentar deserializar a PrintJobRequest
                var request = JsonSerializer.Deserialize<PrintJobRequest>(message, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (request != null && !string.IsNullOrEmpty(request.JobId))
                {
                    _logger.LogInfo($"AndroidWebSocketService: PrintJobRequest deserializado para JobId: {request.JobId}");

                    // Disparar evento de la interfaz IWebSocketService con el nuevo EventArgs que incluye ClientId
                    var eventArgs = new WebSocketMessageReceivedEventArgs<PrintJobRequest>(clientId, request);
                    OnPrintJobReceived?.Invoke(this, eventArgs);
                }
                else
                {
                    // Intentar detectar si es una actualización de plantilla
                    using (JsonDocument doc = JsonDocument.Parse(message))
                    {
                        if (doc.RootElement.TryGetProperty("Action", out JsonElement actionProp) &&
                            string.Equals(actionProp.GetString(), "UpdateTemplate", StringComparison.OrdinalIgnoreCase))
                        {
                            var updateReq = JsonSerializer.Deserialize<UpdateTemplateRequest>(message, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                            if (updateReq?.Template != null)
                            {
                                _logger.LogInfo($"AndroidWebSocketService: UpdateTemplateRequest recibido de {clientId} para {updateReq.Template.DocumentType}");
                                var updateArgs = new WebSocketMessageReceivedEventArgs<PrintTemplate>(clientId, updateReq.Template);
                                OnTemplateUpdateReceived?.Invoke(this, updateArgs);
                                return;
                            }
                        }
                    }

                    _logger.LogWarning($"AndroidWebSocketService: No se pudo procesar el mensaje como PrintJobRequest o UpdateTemplate: {message}");
                }
            }
            catch (JsonException jex)
            {
                _logger.LogError($"AndroidWebSocketService: Error de deserialización JSON: {jex.Message}", jex);
            }
            catch (Exception ex)
            {
                _logger.LogError($"AndroidWebSocketService: Error al procesar mensaje recibido: {ex.Message}", ex);
            }
        }
    }
}
