using AppsielPrintManager.Core.Models;
using System.Threading.Tasks;

namespace AppsielPrintManager.Core.Interfaces
{
    /// <summary>
    /// Define la interfaz para el servicio de impresión, orquestando el proceso completo
    /// desde la recepción de la solicitud hasta el envío a la impresora.
    /// </summary>
    public interface IPrintService
    {
        /// <summary>
        /// Procesa una solicitud de trabajo de impresión.
        /// Incluye parseo, renderizado, generación de comandos ESC/POS y envío a la impresora(s).
        /// </summary>
        /// <param name="request">El objeto PrintJobRequest que contiene los detalles del trabajo de impresión.</param>
        /// <returns>Un PrintJobResult indicando el éxito o fracaso de la operación.</returns>
        Task<PrintJobResult> ProcessPrintJobAsync(PrintJobRequest request);

        /// <summary>
        /// Permite configurar una impresora específica.
        /// </summary>
        /// <param name="settings">La configuración de la impresora a guardar o actualizar.</param>
        /// <returns>Tarea que representa la operación asíncrona.</returns>
        Task ConfigurePrinterAsync(PrinterSettings settings);

        /// <summary>
        /// Obtiene la configuración de una impresora por su identificador.
        /// </summary>
        /// <param name="printerId">El identificador único de la impresora.</param>
        /// <returns>La configuración de la impresora si se encuentra, de lo contrario, null.</returns>
        Task<PrinterSettings> GetPrinterSettingsAsync(string printerId);

        /// <summary>
        /// Obtiene todas las configuraciones de impresoras guardadas.
        /// </summary>
        /// <returns>Una lista de todas las configuraciones de impresoras.</returns>
        Task<List<PrinterSettings>> GetAllPrinterSettingsAsync();

        /// <summary>
        /// Elimina la configuración de una impresora por su identificador.
        /// </summary>
        /// <param name="printerId">El identificador único de la impresora a eliminar.</param>
        /// <returns>Tarea que representa la operación asíncrona.</returns>
        Task DeletePrinterSettingsAsync(string printerId);
    }
}
