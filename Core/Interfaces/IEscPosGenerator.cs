using AppsielPrintManager.Core.Models;
using System.Threading.Tasks;

namespace AppsielPrintManager.Core.Interfaces
{
    /// <summary>
    /// Define la interfaz para el generador de comandos ESC/POS.
    /// Encargado de convertir un objeto TicketContent estructurado en una secuencia
    /// de bytes de comandos ESC/POS listos para ser enviados a la impresora.
    /// </summary>
    public interface IEscPosGenerator
    {
        /// <summary>
        /// Genera una secuencia de bytes de comandos ESC/POS a partir de un TicketContent.
        /// </summary>
        /// <param name="ticketContent">El objeto TicketContent que contiene la disposición del ticket.</param>
        /// <param name="printerSettings">La configuración de la impresora para aplicar estilos específicos o ancho de papel.</param>
        /// <returns>Una matriz de bytes que representa los comandos ESC/POS.</returns>
        Task<byte[]> GenerateEscPosCommandsAsync(TicketContent ticketContent, PrinterSettings printerSettings);
    }
}
