using AppsielPrintManager.Core.Models;
using System.Threading.Tasks;

namespace AppsielPrintManager.Core.Interfaces
{
    /// <summary>
    /// Define la interfaz para el servicio de báscula, específico para plataformas que lo soporten (ej. Windows).
    /// Permite la detección, lectura y configuración de básculas físicas.
    /// </summary>
    public interface IScaleService
    {
        /// <summary>
        /// Evento que se dispara cuando se recibe una nueva lectura estable de la báscula.
        /// </summary>
        event AsyncEventHandler<ScaleData> OnScaleDataReceived;

        /// <summary>
        /// Inicializa el servicio de báscula, intentando detectar y conectar a una báscula.
        /// </summary>
        /// <returns>Tarea que representa la operación asíncrona de inicialización.</returns>
        Task InitializeAsync();

        /// <summary>
        /// Detiene la lectura de la báscula y libera los recursos.
        /// </summary>
        /// <returns>Tarea que representa la operación asíncrona de detención.</returns>
        Task StopAsync();

        /// <summary>
        /// Obtiene la última lectura de peso de la báscula.
        /// </summary>
        /// <returns>El objeto ScaleData con la última lectura.</returns>
        ScaleData GetLastScaleReading();

        /// <summary>
        /// Indica si el servicio de báscula está actualmente conectado y leyendo datos.
        /// </summary>
        bool IsConnected { get; }
    }
}
