using AppsielPrintManager.Core.Models;
using System.Threading.Tasks;

namespace AppsielPrintManager.Core.Interfaces
{
    /// <summary>
    /// Define la interfaz para el servicio de comunicación WebSocket.
    /// Encargado de establecer y mantener la conexión, enviar mensajes y recibir solicitudes de impresión.
    /// </summary>
    public interface IWebSocketService
    {
        /// <summary>
        /// Evento que se dispara cuando se recibe una solicitud de trabajo de impresión a través del WebSocket.
        /// </summary>
        event AsyncEventHandler<PrintJobRequest> OnPrintJobReceived;

        /// <summary>
        /// Inicializa y conecta el cliente WebSocket al servidor especificado.
        /// </summary>
        /// <param name="uri">La URI del servidor WebSocket al que conectarse.</param>
        /// <returns>Tarea que representa la operación asíncrona de conexión.</returns>
        Task ConnectAsync(string uri);

        /// <summary>
        /// Envía un resultado de trabajo de impresión de vuelta al servidor a través del WebSocket.
        /// </summary>
        /// <param name="result">El objeto PrintJobResult que contiene el estado del trabajo.</param>
        /// <returns>Tarea que representa la operación asíncrona de envío.</returns>
        Task SendPrintJobResultAsync(PrintJobResult result);

        /// <summary>
        /// Envía datos de báscula al servidor a través del WebSocket.
        /// </summary>
        /// <param name="scaleData">El objeto ScaleData que contiene la lectura de la báscula.</param>
        /// <returns>Tarea que representa la operación asíncrona de envío.</returns>
        Task SendScaleDataAsync(ScaleData scaleData);

        /// <summary>
        /// Desconecta el cliente WebSocket del servidor.
        /// </summary>
        /// <returns>Tarea que representa la operación asíncrona de desconexión.</returns>
        Task DisconnectAsync();

        /// <summary>
        /// Obtiene el estado actual de la conexión WebSocket.
        /// </summary>
        bool IsConnected { get; }
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
