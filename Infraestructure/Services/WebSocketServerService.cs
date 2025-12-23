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
    /// Este servidor escucha en un puerto específico y acepta una única conexión de cliente
    /// (ej. una página web de Appsiel). Recibe solicitudes de impresión y envía resultados.
    /// </summary>
    public class WebSocketServerService : IWebSocketService
    {
        private readonly ILoggingService _logger;
        private HttpListener _httpListener;
        private CancellationTokenSource _cts;
        private WebSocket _currentWebSocket;
        private ConcurrentQueue<string> _messageQueue;
        private Task _messageProcessingTask;
        private readonly SemaphoreSlim _connectionSemaphore = new SemaphoreSlim(1, 1); // Permite solo 1 conexión a la vez
        private int _connectedClientCount = 0; // Contador de clientes conectados

        /// <summary>
        /// Evento que se dispara cuando un cliente WebSocket se conecta al servidor APM.
        /// El argumento es el ID de la conexión o un identificador de cliente.
        /// </summary>
        public event AsyncEventHandler<string> OnClientConnected;

        /// <summary>
        /// Evento que se dispara cuando un cliente WebSocket se desconecta del servidor APM.
        /// </summary>
        public event AsyncEventHandler<string> OnClientDisconnected;

        /// <summary>
        /// Evento que se dispara cuando se recibe una solicitud de trabajo de impresión desde un cliente conectado.
        /// </summary>
        public event AsyncEventHandler<PrintJobRequest> OnPrintJobReceived;

        /// <summary>
        /// Indica si el servidor WebSocket está actualmente escuchando conexiones.
        /// </summary>
        public bool IsRunning => _httpListener?.IsListening ?? false;

        /// <summary>
        /// Obtiene el número actual de clientes conectados al servidor WebSocket.
        /// (Para este proyecto, se espera 0 o 1).
        /// </summary>
        public int CurrentClientCount => _connectedClientCount;


        /// <summary>
        /// Constructor del servicio WebSocketServerService.
        /// </summary>
        /// <param name="logger">Servicio de logging para registrar eventos.</param>
        public WebSocketServerService(ILoggingService logger)
        {
            _logger = logger;
            _messageQueue = new ConcurrentQueue<string>();
        }

        /// <summary>
        /// Inicia el servidor WebSocket local en el puerto especificado.
        /// Configura un HttpListener para aceptar peticiones HTTP y las "actualiza" a conexiones WebSocket.
        /// </summary>
        /// <param name="port">El número de puerto en el que el servidor escuchará (ej. 7000).</param>
        /// <returns>Tarea que representa la operación asíncrona de inicio del servidor.</returns>
        public async Task StartServerAsync(int port)
        {
            if (IsRunning)
            {
                _logger.LogWarning("El servidor WebSocket ya está en ejecución.");
                return;
            }

            _cts = new CancellationTokenSource();
            _httpListener = new HttpListener();
            // La URL debe terminar en '/' para HttpListener.
            // 'http://*:' permite escuchar en todas las interfaces de red, útil para conexiones desde otros dispositivos.
            // Requiere permisos de administrador o configuración de ACL (netsh http add urlacl).
            _httpListener.Prefixes.Add($"http://*:{port}/websocket/"); 

            try
            {
                _httpListener.Start();
                _logger.LogInfo($"Servidor WebSocket iniciado y escuchando en puerto {port} en http://*:{port}/websocket/");
                // Inicia un bucle para escuchar conexiones entrantes de clientes.
                _ = ListenForConnectionsAsync(_cts.Token); 
                // Inicia una tarea para procesar los mensajes JSON recibidos de forma asíncrona.
                _messageProcessingTask = ProcessMessagesAsync(_cts.Token); 
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al iniciar el servidor WebSocket: {ex.Message}", ex);
                // Si falla el inicio, intentar detener y limpiar recursos.
                // Usamos 'false' para el semáforo para no esperar si no es necesario, ya que es un error de inicio.
                await StopServerAsync(false); 
            }
        }

        /// <summary>
        /// Detiene el servidor WebSocket local y cierra todas las conexiones activas.
        /// Cancela las tareas de escucha y procesamiento de mensajes, y libera los recursos de red.
        /// </summary>
        /// <param name="waitForSemaphore">Indica si se debe esperar por el semáforo de conexión.</param>
        /// <returns>Tarea que representa la operación asíncrona de detención del servidor.</returns>
        private async Task StopServerAsync(bool waitForSemaphore = true)
        {
            if (!IsRunning && _httpListener == null)
            {
                _logger.LogWarning("El servidor WebSocket no está en ejecución.");
                return;
            }

            // Intentar cerrar el WebSocket del cliente de forma limpia si está abierto.
            if (_currentWebSocket != null && _currentWebSocket.State == WebSocketState.Open)
            {
                try
                {
                    await _currentWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Servidor detenido", CancellationToken.None);
                    _logger.LogInfo("WebSocket de cliente cerrado.");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error al cerrar el WebSocket del cliente: {ex.Message}", ex);
                }
                finally
                {
                    _currentWebSocket.Dispose();
                    _currentWebSocket = null;
                }
            }
            
            // Detener HttpListener antes de cancelar CTS para evitar HttpListenerException si el CTS se cancela primero.
            if (_httpListener != null)
            {
                try
                {
                    _httpListener.Stop(); // Detiene la escucha de nuevas peticiones.
                    _httpListener.Close(); // Libera los recursos del HttpListener.
                    _httpListener = null;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error al detener HttpListener: {ex.Message}", ex);
                }
            }

            // Ahora sí, cancelar las tareas en segundo plano.
            _cts?.Cancel(); 

            if (waitForSemaphore)
            {
                await _connectionSemaphore.WaitAsync(); // Esperar a que cualquier procesamiento de conexión actual termine
            }
            
            try
            {
                if (_messageProcessingTask != null)
                {
                    // Esperar a que el procesamiento de mensajes termine de forma segura.
                    // Capturar OperationCanceledException aquí para evitar que se propague.
                    try { await _messageProcessingTask; }
                    catch (OperationCanceledException) { _logger.LogInfo("Procesamiento de mensajes cancelado."); }
                }
                _logger.LogInfo("Servidor WebSocket detenido.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al detener las tareas del servidor WebSocket: {ex.Message}", ex);
            }
            finally
            {
                if (waitForSemaphore) _connectionSemaphore.Release(); // Liberar el semáforo.
                _cts?.Dispose();
                _cts = null;
                _connectedClientCount = 0; // Resetear el contador de clientes.
            }
        }

        // Overload público de StopServerAsync sin parámetros para cumplir con la interfaz.
        public Task StopServerAsync() => StopServerAsync(true);

        /// <summary>
        /// Envía un resultado de trabajo de impresión de vuelta al cliente WebSocket conectado.
        /// Serializa el objeto PrintJobResult a JSON y lo envía como mensaje de texto.
        /// </summary>
        /// <param name="result">El objeto PrintJobResult que contiene el estado del trabajo.</param>
        /// <returns>Tarea que representa la operación asíncrona de envío.</returns>
        public async Task SendPrintJobResultToAllClientsAsync(PrintJobResult result)
        {
            if (_currentWebSocket?.State == WebSocketState.Open)
            {
                await SendMessageAsync(JsonSerializer.Serialize(result));
            }
            else
            {
                _logger.LogWarning("No hay cliente WebSocket conectado o el WebSocket no está abierto para enviar PrintJobResult.");
            }
        }

        /// <summary>
        /// Envía datos de báscula al cliente WebSocket conectado.
        /// Serializa el objeto ScaleData a JSON y lo envía como mensaje de texto.
        /// </summary>
        /// <param name="scaleData">El objeto ScaleData que contiene la lectura de la báscula.</param>
        /// <returns>Tarea que representa la operación asíncrona de envío.</returns>
        public async Task SendScaleDataToAllClientsAsync(ScaleData scaleData)
        {
            if (_currentWebSocket?.State == WebSocketState.Open)
            {
                await SendMessageAsync(JsonSerializer.Serialize(scaleData));
            }
            else
            {
                _logger.LogWarning("No hay cliente WebSocket conectado o el WebSocket no está abierto para enviar ScaleData.");
            }
        }

        // --- Métodos Privados ---

        /// <summary>
        /// Bucle principal que escucha continuamente las peticiones de conexión HTTP.
        /// Cuando detecta una petición de WebSocket, intenta aceptarla y luego maneja la comunicación con el cliente.
        /// Asegura que solo una conexión WebSocket sea gestionada activamente.
        /// </summary>
        /// <param name="cancellationToken">Token de cancelación para detener la escucha.</param>
        private async Task ListenForConnectionsAsync(CancellationToken cancellationToken)
        {
            // El bucle continuará mientras el servidor esté activo y no se haya solicitado la cancelación.
            while (!cancellationToken.IsCancellationRequested && (_httpListener?.IsListening ?? false))
            {
                HttpListenerContext context = null;
                try
                {
                    // Espera una nueva petición HTTP. Esto es un punto de bloqueo hasta que llega una petición.
                    // El ConfigureAwait(false) ayuda a evitar deadlocks en contextos de sincronización específicos (ej. UI).
                    context = await _httpListener.GetContextAsync().ConfigureAwait(false);
                }
                // HttpListener puede lanzar una excepción específica si se detiene mientras espera una conexión.
                catch (HttpListenerException ex) when (ex.ErrorCode == 995) // ERROR_OPERATION_ABORTED
                {
                    // Esto es normal cuando el Stop() o Close() es llamado en HttpListener.
                    _logger.LogInfo("HttpListener fue detenido mientras esperaba conexiones (Operation Aborted).");
                    break; 
                }
                catch (OperationCanceledException)
                {
                    // Capturar OperationCanceledException si el CTS se cancela antes que HttpListener.Stop().
                    _logger.LogInfo("Escucha de conexiones HttpListener cancelada.");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error inesperado al obtener contexto de conexión: {ex.Message}", ex);
                    // Si ocurre un error, continuar el bucle para intentar escuchar otras peticiones.
                    continue; 
                }

                // Verifica si la petición HTTP entrante es una solicitud de establecimiento de WebSocket.
                if (context.Request.IsWebSocketRequest)
                {
                    // Usamos un semáforo para garantizar que solo una conexión sea gestionada a la vez.
                    await _connectionSemaphore.WaitAsync(cancellationToken); 
                    try
                    {
                        // Si ya tenemos un WebSocket activo y abierto, rechazamos la nueva conexión.
                        if (_currentWebSocket != null && _currentWebSocket.State == WebSocketState.Open)
                        {
                            _logger.LogWarning($"Se intentó una nueva conexión WebSocket desde {context.Request.RemoteEndPoint}, pero ya hay una activa. Rechazando nueva conexión.");
                            context.Response.StatusCode = 409; // Código de estado HTTP 409 Conflict.
                            context.Response.StatusDescription = "Ya hay una conexión activa. Cierre la anterior para establecer una nueva.";
                            context.Response.Close();
                            _connectionSemaphore.Release(); // Liberar el semáforo inmediatamente.
                            continue; // Continuar escuchando para más peticiones.
                        }

                        _logger.LogInfo($"Petición de WebSocket recibida desde {context.Request.RemoteEndPoint}. Estableciendo conexión...");
                        HttpListenerWebSocketContext wsContext = null;
                        try
                        {
                            // Acepta la solicitud de WebSocket, actualizando la conexión HTTP a una conexión WebSocket.
                            wsContext = await context.AcceptWebSocketAsync(subProtocol: null).ConfigureAwait(false);
                            _currentWebSocket = wsContext.WebSocket;
                            // Usamos la dirección IP del cliente como un ID simple para logs.
                            var clientId = context.Request.RemoteEndPoint.ToString(); 

                            _logger.LogInfo($"Cliente WebSocket conectado: {clientId}");
                            Interlocked.Increment(ref _connectedClientCount); // Incrementar el contador de clientes
                            // Disparar evento de conexión de cliente.
                            OnClientConnected?.Invoke(this, clientId).Forget();

                            // Una vez conectado, este método comienza a escuchar los mensajes del cliente.
                            await ReceiveMessagesAsync(_currentWebSocket, clientId, cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Error al establecer o manejar el WebSocket con {context.Request.RemoteEndPoint}: {ex.Message}", ex);
                            // Intentar cerrar la conexión si falló la negociación o el manejo.
                            if (wsContext != null && wsContext.WebSocket.State == WebSocketState.Open)
                            {
                                await wsContext.WebSocket.CloseAsync(WebSocketCloseStatus.InternalServerError, ex.Message, CancellationToken.None);
                            }
                        }
                        finally
                        {
                            // Este bloque se ejecuta cuando el cliente se desconecta o hay un error en ReceiveMessagesAsync.
                            if (_currentWebSocket != null)
                            {
                                var clientId = context.Request.RemoteEndPoint.ToString();
                                Interlocked.Decrement(ref _connectedClientCount); // Decrementar el contador de clientes
                                // Disparar evento de desconexión de cliente.
                                OnClientDisconnected?.Invoke(this, clientId).Forget();
                                _logger.LogInfo($"Cliente WebSocket {clientId} desconectado. Estado: {_currentWebSocket.State}");
                                _currentWebSocket.Dispose(); // Liberar recursos del WebSocket.
                                _currentWebSocket = null;
                            }
                            _connectionSemaphore.Release(); // Liberar el semáforo para permitir nuevas conexiones si se desea.
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // Capturar esta excepción significa que la escucha fue cancelada intencionalmente.
                        _logger.LogInfo("Operación de conexión WebSocket cancelada.");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error en el semáforo o flujo de conexión: {ex.Message}", ex);
                    }
                }
                else
                {
                    // Si la petición no es de WebSocket, se rechaza.
                    _logger.LogWarning($"Petición HTTP no-WebSocket recibida desde {context.Request.RemoteEndPoint}. Cerrando.");
                    context.Response.StatusCode = 400; // Bad Request.
                    context.Response.StatusDescription = "Solo se aceptan conexiones WebSocket en esta ruta.";
                    context.Response.Close();
                }
            }
            _logger.LogInfo("HttpListener ha dejado de escuchar conexiones HTTP.");
        }

        /// <summary>
        /// Bucle que escucha y recibe mensajes de un cliente WebSocket conectado.
        /// Los mensajes de texto se encolan para su procesamiento asíncrono.
        /// </summary>
        /// <param name="webSocket">El WebSocket del cliente desde el que se recibirán los mensajes.</param>
        /// <param name="clientId">El ID del cliente conectado.</param>
        /// <param name="cancellationToken">Token de cancelación para detener la escucha.</param>
        private async Task ReceiveMessagesAsync(WebSocket webSocket, string clientId, CancellationToken cancellationToken)
        {
            var buffer = new byte[1024 * 4]; // Buffer para almacenar los datos recibidos (4KB).
            _logger.LogInfo($"Comenzando a escuchar mensajes del cliente: {clientId}");

            try
            {
                WebSocketReceiveResult result;
                // Continuar recibiendo mensajes mientras el WebSocket esté abierto y no se haya solicitado la cancelación.
                do
                {
                    if (webSocket.State != WebSocketState.Open) break; // Salir si el WebSocket ya no está abierto.

                    // Espera a recibir datos del cliente.
                    result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken).ConfigureAwait(false);

                    // Si el mensaje es de texto y tiene contenido.
                    if (result.MessageType == WebSocketMessageType.Text && result.Count > 0)
                    {
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        _logger.LogInfo($"Mensaje de texto recibido de {clientId}: {message}");
                        // Encolar el mensaje para ser procesado por otra tarea, evitando bloquear la recepción.
                        _messageQueue.Enqueue(message); 
                    }
                    // Si el cliente solicita cerrar la conexión.
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        _logger.LogInfo($"Solicitud de cierre de WebSocket recibida de {clientId}. Estado: {result.CloseStatusDescription}");
                        // Responder cerrando la conexión de forma limpia.
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", cancellationToken);
                        break; 
                    }
                } while (!result.CloseStatus.HasValue && !cancellationToken.IsCancellationRequested); // Continuar hasta que la conexión se cierre o se cancele.
            }
            catch (OperationCanceledException)
            {
                // Capturar esta excepción significa que la recepción fue cancelada intencionalmente.
                _logger.LogInfo($"Recepción de mensajes para {clientId} cancelada.");
            }
            catch (WebSocketException wsex)
            {
                // Manejar cierres inesperados o errores de protocolo.
                // Los códigos de error pueden indicar la naturaleza del problema.
                // WebSocketErrorCode.ConnectionClosedPrematurely a menudo ocurre cuando el cliente se desconecta abruptamente.
                if (wsex.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely || webSocket.State == WebSocketState.Aborted)
                {
                    _logger.LogWarning($"El cliente {clientId} cerró la conexión WebSocket de forma inesperada (ej. recarga de página). Estado: {webSocket.State}, Código de Error: {wsex.WebSocketErrorCode}");
                }
                else
                {
                    _logger.LogError($"Error de WebSocket al recibir mensajes de {clientId}: {wsex.Message} (Código: {wsex.WebSocketErrorCode})", wsex);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error inesperado al recibir mensajes de {clientId}: {ex.Message}", ex);
            }
            _logger.LogInfo($"Finalizada la escucha de mensajes para el cliente: {clientId}.");
        }

        /// <summary>
        /// Tarea de procesamiento en segundo plano que toma mensajes de la cola y los procesa.
        /// Deserializa mensajes JSON a objetos PrintJobRequest y dispara el evento OnPrintJobReceived.
        /// </summary>
        /// <param name="cancellationToken">Token de cancelación para detener el procesamiento.</param>
        private async Task ProcessMessagesAsync(CancellationToken cancellationToken)
        {
            // El bucle continuará mientras no se haya solicitado la cancelación.
            while (!cancellationToken.IsCancellationRequested)
            {
                // Intenta desencolar un mensaje.
                if (_messageQueue.TryDequeue(out string message))
                {
                    try
                    {
                        // Intenta deserializar el mensaje JSON a un PrintJobRequest.
                        var request = JsonSerializer.Deserialize<PrintJobRequest>(message, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        if (request != null)
                        {
                            _logger.LogInfo($"PrintJobRequest deserializado para JobId: {request.JobId}. Disparando evento OnPrintJobReceived.");
                            // Disparar el evento para que los suscriptores (ej. la UI) procesen la solicitud.
                            OnPrintJobReceived?.Invoke(this, request).Forget();
                        }
                    }
                    catch (JsonException jex)
                    {
                        _logger.LogError($"Error de deserialización JSON: {jex.Message}. Mensaje: '{message}'", jex);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error al procesar mensaje de la cola: {ex.Message}. Mensaje: '{message}'", ex);
                    }
                }
                else
                {
                    // Si no hay mensajes en la cola, esperar un breve período para no consumir CPU.
                    await Task.Delay(100, cancellationToken); 
                }
            }
            _logger.LogInfo("Procesamiento de mensajes en cola detenido.");
        }

        /// <summary>
        /// Envía un mensaje de texto a través del WebSocket al cliente actualmente conectado.
        /// </summary>
        /// <param name="message">El mensaje de texto a enviar.</param>
        private async Task SendMessageAsync(string message)
        {
            // Solo enviar si hay un WebSocket conectado y su estado es "Open".
            if (_currentWebSocket?.State == WebSocketState.Open)
            {
                try
                {
                    var buffer = Encoding.UTF8.GetBytes(message);
                    // Envía el mensaje como un fragmento de texto completo.
                    await _currentWebSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
                    _logger.LogInfo($"Mensaje enviado al cliente WebSocket: {message}");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error al enviar mensaje a través de WebSocket: {ex.Message}", ex);
                }
            }
        }
    }

    /// <summary>
    /// Extensión para métodos asíncronos que no necesitan ser esperados.
    /// Esto permite iniciar una tarea asíncrona ("fire-and-forget") sin que el código llamador
    /// tenga que esperar su finalización, evitando advertencias del compilador.
    /// Es crucial manejar las excepciones que puedan ocurrir en estas tareas olvidadas.
    /// </summary>
    /// <param name="task">La tarea asíncrona a ejecutar y olvidar.</param>
    internal static class TaskExtensions
    {
        public static void Forget(this Task task)
        {
            // Este método adjunta una continuación a la tarea que solo se ejecutará si la tarea falla.
            // Si la tarea falla y no se maneja la excepción, podría terminar el proceso.
            // Aquí, simplemente registramos la excepción para depuración. En un entorno de producción,
            // se usaría un sistema de logging más robusto.
            task.ContinueWith(t =>
            {
                if (t.IsFaulted && t.Exception != null)
                {
                    // Este Console.Error.WriteLine es un marcador de posición.
                    // En una aplicación real, se usaría un ILoggingService inyectado o un manejador global de excepciones.
                    Console.Error.WriteLine($"[ERROR - Tarea Olvidada] Excepción: {t.Exception.Message}");
                    Console.Error.WriteLine($"[ERROR - Tarea Olvidada] StackTrace: {t.Exception.StackTrace}");
                }
            }, TaskContinuationOptions.OnlyOnFaulted);
        }
    }
}
