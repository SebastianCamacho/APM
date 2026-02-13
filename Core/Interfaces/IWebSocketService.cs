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
    /// <summary>
    /// Argumentos para el evento de mensaje recibido via WebSocket.
    /// </summary>
    /// <typeparam name="T">Tipo del mensaje recibido.</typeparam>
    public class WebSocketMessageReceivedEventArgs<T> : EventArgs
    {
        public string ClientId { get; }
        public T Message { get; }

        public WebSocketMessageReceivedEventArgs(string clientId, T message)
        {
            ClientId = clientId;
            Message = message;
        }
    }

    public delegate Task AsyncEventHandler<TEventArgs>(object sender, TEventArgs e);

    /// <summary>
    /// Define la interfaz para el servicio WebSocket del APM.
    /// </summary>
    public interface IWebSocketService
    {
        event AsyncEventHandler<string> OnClientConnected;
        event AsyncEventHandler<string> OnClientDisconnected;

        // Actualizado para incluir ClientId
        event AsyncEventHandler<WebSocketMessageReceivedEventArgs<PrintJobRequest>> OnPrintJobReceived;

        /// <summary>
        /// Evento que se dispara cuando se recibe una solicitud de actualización de plantilla.
        /// </summary>
        event AsyncEventHandler<WebSocketMessageReceivedEventArgs<PrintTemplate>> OnTemplateUpdateReceived;

        Task StartServerAsync(int port);
        Task StopServerAsync();

        Task SendPrintJobResultToAllClientsAsync(PrintJobResult result);

        // Nuevo método para unicast
        Task SendPrintJobResultToClientAsync(string clientId, PrintJobResult result);

        Task SendScaleDataToAllClientsAsync(ScaleData scaleData);

        /// <summary>
        /// Envía el resultado de una actualización de plantilla a un cliente específico.
        /// </summary>
        Task SendTemplateUpdateResultAsync(string clientId, TemplateUpdateResult result);

        bool IsRunning { get; }
        int CurrentClientCount { get; }
    }
}
