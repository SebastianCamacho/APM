using System.Threading.Tasks;

namespace AppsielPrintManager.Core.Interfaces
{
    /// <summary>
    /// Define la interfaz para gestionar el ciclo de vida del Worker Service.
    /// Esto permite a la UI iniciar, detener y monitorizar el Worker Service externo.
    /// </summary>
    public interface IWorkerServiceManager
    {
        /// <summary>
        /// Inicia el Worker Service externo.
        /// </summary>
        /// <returns>True si el servicio se inició correctamente o ya estaba en ejecución; de lo contrario, false.</returns>
        Task<bool> StartWorkerServiceAsync();

        /// <summary>
        /// Detiene el Worker Service externo.
        /// </summary>
        /// <returns>True si el servicio se detuvo correctamente o ya estaba detenido; de lo contrario, false.</returns>
        Task<bool> StopWorkerServiceAsync();

        /// <summary>
        /// Verifica si el Worker Service externo está en ejecución.
        /// </summary>
        bool IsWorkerServiceRunning { get; }
    }
}
