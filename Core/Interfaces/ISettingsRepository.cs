using AppsielPrintManager.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AppsielPrintManager.Core.Interfaces
{
    /// <summary>
    /// Define la interfaz para el repositorio de configuración de impresoras.
    /// Encargado de almacenar, recuperar y gestionar las configuraciones de PrinterSettings.
    /// </summary>
    public interface ISettingsRepository
    {
        /// <summary>
        /// Guarda o actualiza la configuración de una impresora.
        /// Si la impresora ya existe (mismo PrinterId), se actualiza. De lo contrario, se añade.
        /// </summary>
        /// <param name="settings">La configuración de la impresora a guardar.</param>
        /// <returns>Tarea que representa la operación asíncrona.</returns>
        Task SavePrinterSettingsAsync(PrinterSettings settings);

        /// <summary>
        /// Obtiene la configuración de una impresora por su identificador único.
        /// </summary>
        /// <param name="printerId">El identificador de la impresora.</param>
        /// <returns>La configuración de la impresora si se encuentra, de lo contrario, null.</returns>
        Task<PrinterSettings> GetPrinterSettingsAsync(string printerId);

        /// <summary>
        /// Obtiene todas las configuraciones de impresoras almacenadas.
        /// </summary>
        /// <returns>Una lista de todas las configuraciones de impresoras.</returns>
        Task<List<PrinterSettings>> GetAllPrinterSettingsAsync();

        /// <summary>
        /// Elimina la configuración de una impresora por su identificador único.
        /// </summary>
        /// <param name="printerId">El identificador de la impresora a eliminar.</param>
        /// <returns>True si la impresora fue eliminada exitosamente, false si no se encontró.</returns>
        Task<bool> DeletePrinterSettingsAsync(string printerId);
    }
}
