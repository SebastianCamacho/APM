using AppsielPrintManager.Core.Models;
using System.Threading.Tasks;

namespace AppsielPrintManager.Core.Interfaces
{
    /// <summary>
    /// Define la interfaz para el renderizador de tickets.
    /// Es responsable de tomar los datos de un PrintJobRequest y transformarlos
    /// en una estructura de TicketContent que representa la disposición visual del ticket.
    /// </summary>
    public interface ITicketRenderer
    {
        /// <summary>
        /// Renderiza un PrintJobRequest en un objeto TicketContent estructurado.
        /// Este proceso implica el parseo del JSON, la validación, la construcción del layout
        /// (encabezados, ítems, totales, pies de página) y la inserción de multimedia (imágenes, códigos de barras, QR).
        /// </summary>
        /// <param name="request">El objeto PrintJobRequest que contiene los datos del ticket.</param>
        /// <returns>Un objeto TicketContent que representa la estructura del ticket listo para ser convertido a comandos de impresora.</returns>
        Task<TicketContent> RenderTicketAsync(PrintJobRequest request, object documentData);
    }
}
