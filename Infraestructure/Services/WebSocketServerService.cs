using AppsielPrintManager.Core.Interfaces;
using AppsielPrintManager.Core.Models;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AppsielPrintManager.Infraestructure.Services
{
    /// <summary>
    /// Implementación del servicio WebSocket que actúa como un servidor local.
    /// Este servidor escucha en un puerto específico y acepta múltiples conexiones de clientes.
    /// </summary>
    public class WebSocketServerService : IWebSocketService
    {
        private readonly ILoggingService _logger;
        private HttpListener? _httpListener;
        private CancellationTokenSource? _cts;

        // Almacena los clientes conectados: Key = ClientID, Value = WebSocket
        private ConcurrentDictionary<string, WebSocket> _connectedClients;

        // Cola de mensajes recibidos: Tupla (ClientId, Message)
        private ConcurrentQueue<(string ClientId, string Message)> _messageQueue;

        private Task? _messageProcessingTask;

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
        /// Indica si el servidor WebSocket está actualmente escuchando conexiones.
        /// </summary>
        public bool IsRunning => _httpListener?.IsListening ?? false;

        /// <summary>
        /// Obtiene el número actual de clientes conectados al servidor WebSocket.
        /// </summary>
        public int CurrentClientCount => _connectedClients.Count;

        private readonly IPrintService _printService;
        private readonly IScaleService _scaleService;

        // Diccionario para mapear ClientID -> ScaleId que está escuchando
        private ConcurrentDictionary<string, string> _clientScaleSubscriptions = new();

        public WebSocketServerService(ILoggingService logger, IPrintService printService, IScaleService scaleService)
        {
            _logger = logger;
            _printService = printService;
            _scaleService = scaleService;
            _connectedClients = new ConcurrentDictionary<string, WebSocket>();
            _messageQueue = new ConcurrentQueue<(string, string)>();

            // Suscribirse al evento de peso
            _scaleService.OnWeightChanged += ScaleService_OnWeightChanged;
        }

        private void ScaleService_OnWeightChanged(object? sender, ScaleDataEventArgs e)
        {
            // Enviar solo a los clientes suscritos a esta báscula
            var json = JsonSerializer.Serialize(e.Data);
            var buffer = Encoding.UTF8.GetBytes(json);
            var segment = new ArraySegment<byte>(buffer);

            foreach (var subscription in _clientScaleSubscriptions)
            {
                if (subscription.Value.Equals(e.ScaleId, StringComparison.OrdinalIgnoreCase))
                {
                    if (_connectedClients.TryGetValue(subscription.Key, out var ws) && ws.State == WebSocketState.Open)
                    {
                        // Fire and forget send
                        ws.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None).Forget();
                    }
                }
            }
        }

        /// <summary>
        /// Inicia el servidor WebSocket local en el puerto especificado.
        /// </summary>
        public async Task StartServerAsync(int port)
        {
            if (IsRunning)
            {
                _logger.LogWarning("El servidor WebSocket ya está en ejecución.");
                return;
            }

            _cts = new CancellationTokenSource();
            _httpListener = new HttpListener();
            _httpListener.Prefixes.Add($"http://localhost:{port}/websocket/");

            try
            {
                _httpListener.Start();
                _logger.LogInfo($"Servidor WebSocket iniciado y escuchando en puerto {port} en http://localhost:{port}/websocket/");

                // Inicializar servicio de básculas
                await _scaleService.InitializeAsync();

                _ = ListenForConnectionsAsync(_cts.Token);
                _messageProcessingTask = ProcessMessagesAsync(_cts.Token);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al iniciar el servidor WebSocket: {ex.Message}", ex);
                await StopServerAsync();
            }
        }

        /// <summary>
        /// Detiene el servidor WebSocket local y cierra todas las conexiones activas.
        /// </summary>
        public async Task StopServerAsync()
        {
            if (!IsRunning && _httpListener == null)
            {
                _logger.LogWarning("El servidor WebSocket no está en ejecución.");
                return;
            }

            // Detener HttpListener primero
            if (_httpListener != null)
            {
                try
                {
                    _httpListener.Stop();
                    _httpListener.Close();
                    _httpListener = null;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error al detener HttpListener: {ex.Message}", ex);
                }
            }

            _cts?.Cancel();

            // Cerrar todos los WebSockets de clientes
            foreach (var client in _connectedClients)
            {
                var clientId = client.Key;
                var webSocket = client.Value;

                if (webSocket.State == WebSocketState.Open)
                {
                    try
                    {
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Servidor detenido", CancellationToken.None);
                    }
                    catch { }
                }
                webSocket.Dispose();
            }
            _connectedClients.Clear();
            _clientScaleSubscriptions.Clear();

            try
            {
                if (_messageProcessingTask != null)
                {
                    try { await _messageProcessingTask; }
                    catch (OperationCanceledException) { }
                }
                _logger.LogInfo("Servidor WebSocket detenido.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al detener las tareas del servidor WebSocket: {ex.Message}", ex);
            }
            finally
            {
                _cts?.Dispose();
                _cts = null;
            }
        }

        /// <summary>
        /// Envía un resultado de trabajo de impresión de vuelta a todos los clientes WebSocket conectados.
        /// </summary>
        public async Task SendPrintJobResultToAllClientsAsync(PrintJobResult result)
        {
            var json = JsonSerializer.Serialize(result);
            foreach (var client in _connectedClients)
            {
                if (client.Value.State == WebSocketState.Open)
                {
                    await SendMessageAsync(client.Value, json);
                }
            }
        }

        /// <summary>
        /// Envía un resultado de trabajo de impresión a un cliente específico.
        /// </summary>
        public async Task SendPrintJobResultToClientAsync(string clientId, PrintJobResult result)
        {
            if (_connectedClients.TryGetValue(clientId, out var webSocket))
            {
                if (webSocket.State == WebSocketState.Open)
                {
                    await SendMessageAsync(webSocket, JsonSerializer.Serialize(result));
                }
                else
                {
                    _logger.LogWarning($"El socket del cliente {clientId} no está abierto.");
                }
            }
            else
            {
                _logger.LogWarning($"Cliente {clientId} no encontrado para enviar respuesta Unicast.");
            }
        }

        /// <summary>
        /// Envía datos de báscula a todos los clientes WebSocket conectados.
        /// </summary>
        public async Task SendScaleDataToAllClientsAsync(ScaleData scaleData)
        {
            var json = JsonSerializer.Serialize(scaleData);
            foreach (var client in _connectedClients)
            {
                if (client.Value.State == WebSocketState.Open)
                {
                    await SendMessageAsync(client.Value, json);
                }
            }
        }

        // --- Métodos Privados ---

        private async Task ListenForConnectionsAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && (_httpListener?.IsListening ?? false))
            {
                try
                {
                    var context = await _httpListener.GetContextAsync().ConfigureAwait(false);

                    if (context.Request.Url.AbsolutePath.EndsWith("/status", StringComparison.OrdinalIgnoreCase))
                    {
                        var status = new
                        {
                            IsRunning = true,
                            ConnectedClients = CurrentClientCount,
                            ScaleStatuses = _scaleService.GetAllStatuses()
                        };
                        var json = JsonSerializer.Serialize(status);
                        var buffer = Encoding.UTF8.GetBytes(json);

                        context.Response.ContentType = "application/json";
                        context.Response.ContentLength64 = buffer.Length;
                        await context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                        context.Response.Close();
                        continue;
                    }

                    if (context.Request.Url.AbsolutePath.EndsWith("/reload-scales", StringComparison.OrdinalIgnoreCase))
                    {
                        await _scaleService.ReloadScalesAsync();

                        var responseString = "Scales reloaded";
                        var buffer = Encoding.UTF8.GetBytes(responseString);
                        context.Response.ContentType = "text/plain";
                        context.Response.ContentLength64 = buffer.Length;
                        await context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                        context.Response.Close();
                        continue;
                    }

                    if (context.Request.IsWebSocketRequest)
                    {
                        // No esperamos semáforo, permitimos concurrencia
                        _ = ProcessWebSocketConnection(context, cancellationToken);
                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                        context.Response.Close();
                    }
                }
                catch (HttpListenerException ex) when (ex.ErrorCode == 995)
                {
                    break; // Operación abortada
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error aceptando conexión HTTP: {ex.Message}", ex);
                }
            }
        }

        private async Task ProcessWebSocketConnection(HttpListenerContext context, CancellationToken cancellationToken)
        {
            string clientId = context.Request.RemoteEndPoint.ToString();
            WebSocket? webSocket = null;

            try
            {
                var wsContext = await context.AcceptWebSocketAsync(null).ConfigureAwait(false);
                webSocket = wsContext.WebSocket;

                _connectedClients.TryAdd(clientId, webSocket);
                _logger.LogInfo($"Cliente conectado: {clientId}. Total: {CurrentClientCount}");
                OnClientConnected?.Invoke(this, clientId).Forget();

                await ReceiveMessagesAsync(webSocket, clientId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error manejando conexión WebSocket para {clientId}: {ex.Message}", ex);
            }
            finally
            {
                if (webSocket != null)
                {
                    _connectedClients.TryRemove(clientId, out _);
                    // Remover suscripciones
                    if (_clientScaleSubscriptions.TryRemove(clientId, out var scaleId))
                    {
                        _scaleService.StopListening(scaleId);
                    }

                    _logger.LogInfo($"Cliente desconectado: {clientId}. Total: {CurrentClientCount}");
                    OnClientDisconnected?.Invoke(this, clientId).Forget();
                    webSocket.Dispose();
                }
            }
        }

        private async Task ReceiveMessagesAsync(WebSocket webSocket, string clientId, CancellationToken cancellationToken)
        {
            var buffer = new byte[1024 * 4];

            try
            {
                while (webSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
                {
                    using (var ms = new System.IO.MemoryStream())
                    {
                        WebSocketReceiveResult result;
                        do
                        {
                            result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

                            if (result.MessageType == WebSocketMessageType.Close)
                            {
                                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", cancellationToken);
                                return;
                            }

                            if (result.Count > 0)
                            {
                                ms.Write(buffer, 0, result.Count);
                            }

                        } while (!result.EndOfMessage && !cancellationToken.IsCancellationRequested);

                        if (result.MessageType == WebSocketMessageType.Text && ms.Length > 0)
                        {
                            var message = Encoding.UTF8.GetString(ms.ToArray());
                            // _logger.LogInfo($"Mensaje completo recibido de {clientId}. Longitud: {message.Length}"); // Log opcional para debug
                            _messageQueue.Enqueue((clientId, message));
                        }
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _logger.LogError($"Error recibiendo mensajes de {clientId}: {ex.Message}", ex);
            }
        }

        private async Task ProcessMessagesAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (_messageQueue.TryDequeue(out var item))
                {
                    var (clientId, message) = item;
                    try
                    {
                        // Intentar detectar tipo de mensaje
                        // 1. comando de báscula (StartListening/StopListening)
                        // 2. PrintJobRequest

                        using (JsonDocument doc = JsonDocument.Parse(message))
                        {
                            if (doc.RootElement.TryGetProperty("Action", out JsonElement actionElement))
                            {
                                string? action = actionElement.GetString();

                                if (!string.IsNullOrEmpty(action))
                                {
                                    if (string.Equals(action, "StartListening", StringComparison.OrdinalIgnoreCase))
                                    {
                                        if (doc.RootElement.TryGetProperty("ScaleId", out JsonElement scaleIdElem))
                                        {
                                            string? scaleId = scaleIdElem.GetString();
                                            if (!string.IsNullOrEmpty(scaleId))
                                            {
                                                _clientScaleSubscriptions[clientId] = scaleId;
                                                _scaleService.StartListening(scaleId);
                                            }
                                        }
                                    }
                                    else if (string.Equals(action, "StopListening", StringComparison.OrdinalIgnoreCase))
                                    {
                                        if (_clientScaleSubscriptions.TryRemove(clientId, out var scaleId))
                                        {
                                            _scaleService.StopListening(scaleId);
                                        }
                                    }
                                }
                                continue; // Mensaje procesado
                            }
                        }

                        var request = JsonSerializer.Deserialize<PrintJobRequest>(message, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        if (request != null && !string.IsNullOrEmpty(request.JobId))
                        {
                            _logger.LogInfo($"PrintJobRequest deserializado para JobId: {request.JobId} de {clientId}. Iniciando procesamiento.");

                            // 1. Procesar el trabajo de impresión
                            PrintJobResult printResult = await _printService.ProcessPrintJobAsync(request);
                            _logger.LogInfo($"Procesamiento de PrintJobRequest para JobId: {request.JobId} completado con estado: {printResult.Status}.");

                            // 2. Enviar resultado SOLO al cliente solicitante (Unicast)
                            await SendPrintJobResultToClientAsync(clientId, printResult);
                            _logger.LogInfo($"Resultado enviado a {clientId}.");

                            // 3. Disparar evento para notificación externa
                            var eventArgs = new WebSocketMessageReceivedEventArgs<PrintJobRequest>(clientId, request);
                            OnPrintJobReceived?.Invoke(this, eventArgs);
                        }
                    }
                    catch (JsonException jex)
                    {
                        _logger.LogError($"Error de deserialización JSON: {jex.Message}. Mensaje: '{message}'", jex);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error al procesar mensaje de {clientId}: {ex.Message}", ex);
                    }
                }
                else
                {
                    await Task.Delay(100, cancellationToken);
                }
            }
            _logger.LogInfo("Procesamiento de mensajes en cola detenido.");
        }

        private async Task SendMessageAsync(WebSocket webSocket, string message)
        {
            try
            {
                var buffer = Encoding.UTF8.GetBytes(message);
                await webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
            }
            catch (Exception ex)
            {
                // Silently swallow disconnected errors
                _logger.LogWarning($"No se pudo enviar mensaje a cliente: {ex.Message}");
            }
        }
    }

    internal static class TaskExtensions
    {
        public static void Forget(this Task task)
        {
            task.ContinueWith(t =>
            {
                if (t.IsFaulted && t.Exception != null)
                {
                    Console.Error.WriteLine($"[ERROR - Tarea Olvidada] Excepción: {t.Exception.Message}");
                    Console.Error.WriteLine($"[ERROR - Tarea Olvidada] StackTrace: {t.Exception.StackTrace}");
                }
            }, TaskContinuationOptions.OnlyOnFaulted);
        }
    }
}
