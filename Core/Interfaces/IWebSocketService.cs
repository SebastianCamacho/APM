using AppsielPrintManager.Core.Models;
using System.Threading.Tasks;
using System.Net.WebSockets; // Necesario para WebSocketCloseStatus y WebSocketReceiveResult si se usa en la implementación

namespace AppsielPrintManager.Core.Interfaces
{
    /// <summary>
    /// Define la interfaz para el servicio WebSocket del APM, actuando como un servidor local.
    /// Encargado de escuchar conexiones de clientes (ej. páginas web de Appsiel),
    /// recibir solicitudes de impresión y enviar resultados o datos de báscula a los clientes conectados.
    /// </summary>
    public interface IWebSocketService
    {
        /// <summary>
        /// Evento que se dispara cuando un cliente WebSocket se conecta al servidor APM.
        /// El argumento puede ser un identificador de cliente o el objeto WebSocket mismo.
        /// </summary>
        event AsyncEventHandler<string> OnClientConnected; // string podría ser un ID de conexión o similar

        /// <summary>
        /// Evento que se dispara cuando un cliente WebSocket se desconecta del servidor APM.
        /// </summary>
        event AsyncEventHandler<string> OnClientDisconnected; // string podría ser un ID de conexión o similar

        /// <summary>
        /// Evento que se dispara cuando se recibe una solicitud de trabajo de impresión desde un cliente conectado.
        /// </summary>
        event AsyncEventHandler<PrintJobRequest> OnPrintJobReceived;

        /// <summary>
        /// Inicia el servidor WebSocket local en el puerto especificado, comenzando a escuchar nuevas conexiones de clientes.
        /// </summary>
        /// <param name="port">El número de puerto en el que el servidor escuchará.</param>
        /// <returns>Tarea que representa la operación asíncrona de inicio del servidor.</returns>
        Task StartServerAsync(int port);

        /// <summary>
        /// Detiene el servidor WebSocket local, cerrando todas las conexiones de clientes activas.
        /// </summary>
        /// <returns>Tarea que representa la operación asíncrona de detención del servidor.</returns>
        Task StopServerAsync();

        /// <summary>
        /// Envía un resultado de trabajo de impresión de vuelta a todos los clientes WebSocket conectados.
        /// </summary>
        /// <param name="result">El objeto PrintJobResult que contiene el estado del trabajo.</param>
        /// <returns>Tarea que representa la operación asíncrona de envío.</returns>
        Task SendPrintJobResultToAllClientsAsync(PrintJobResult result);

        /// <summary>
        /// Envía datos de báscula a todos los clientes WebSocket conectados.
        /// </summary>
        /// <param name="scaleData">El objeto ScaleData que contiene la lectura de la báscula.</param>
        /// <returns>Tarea que representa la operación asíncrona de envío.</returns>
        Task SendScaleDataToAllClientsAsync(ScaleData scaleData);

        /// <summary>
        /// Obtiene el estado actual del servidor WebSocket (ej. si está escuchando conexiones).
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// Obtiene el número actual de clientes conectados al servidor WebSocket.
        /// (Para este proyecto, se espera 0 o 1).
        /// </summary>
        public int CurrentClientCount { get; }
    }

    /// <summary>
    /// Delegado para eventos asíncronos que reciben un argumento.
    /// </summary>
    /// <typeparam name="TEventArgs">Tipo de los argumentos del evento.</typeparam>
    /// <param name="sender">Origen del evento.</param>
    /// <param name="e">Argumentos del evento.</param>
    /// <returns>Tarea que representa la operación asíncrona.</returns>
    public delegate Task AsyncEventHandler<TEventArgs>(object sender, TEventArgs e);
}
