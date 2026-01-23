using System.Threading.Tasks;

namespace AppsielPrintManager.Core.Interfaces
{
    /// <summary>
    /// Define una interfaz general para servicios específicos de la plataforma.
    /// Esto incluye funcionalidades como la gestión de tareas en segundo plano,
    /// notificaciones, y otras operaciones que varían entre Android y Windows.
    /// </summary>
    public interface IPlatformService
    {
        /// <summary>
        /// Inicia la ejecución de tareas en segundo plano en la plataforma actual.
        /// Por ejemplo, en Android, podría iniciar un Foreground Service.
        /// En Windows, podría registrar y gestionar una tarea en segundo plano.
        /// </summary>
        /// <returns>Tarea que representa la operación asíncrona de inicio del servicio en segundo plano.</returns>
        Task StartBackgroundServiceAsync();

        /// <summary>
        /// Detiene la ejecución de tareas en segundo plano en la plataforma actual.
        /// </summary>
        /// <returns>Tarea que representa la operación asíncrona de detención del servicio en segundo plano.</returns>
        Task StopBackgroundServiceAsync();

        /// <summary>
        /// Muestra una notificación al usuario, útil para informar sobre el estado de la aplicación
        /// o eventos importantes, especialmente cuando la aplicación corre en segundo plano.
        /// </summary>
        /// <param name="title">Título de la notificación.</param>
        /// <param name="message">Cuerpo del mensaje de la notificación.</param>
        void ShowNotification(string title, string message);

        /// <summary>
        /// Obtiene o establece si el servicio en segundo plano está actualmente en ejecución.
        /// </summary>
        bool IsBackgroundServiceRunning { get; }

        /// <summary>
        /// Obtiene si el servidor WebSocket está actualmente en ejecución.
        /// </summary>
        bool IsWebSocketServerRunning { get; }

        /// <summary>
        /// Obtiene el número actual de clientes conectados al servidor WebSocket.
        /// </summary>
        int CurrentClientCount { get; }
    }
}
