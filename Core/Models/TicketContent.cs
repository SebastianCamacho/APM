using System.Collections.Generic;

namespace AppsielPrintManager.Core.Models
{
    /// <summary>
    /// Representa el contenido estructurado de un ticket después de ser parseado
    /// y antes de ser convertido a comandos ESC/POS para la impresión.
    /// Este modelo abstrae la disposición visual del ticket.
    /// </summary>
    public class TicketContent
    {
        /// <summary>
        /// Lista de elementos que conforman el encabezado del ticket.
        /// Pueden ser textos, imágenes, etc.
        /// </summary>
        public List<object> HeaderElements { get; set; } = new List<object>();

        /// <summary>
        /// Lista de filas de la tabla de ítems del ticket.
        /// Cada fila puede contener celdas con texto o datos.
        /// </summary>
        public List<List<object>> ItemTableRows { get; set; } = new List<List<object>>();

        /// <summary>
        /// Lista de elementos que muestran los totales de la venta.
        /// </summary>
        public List<object> TotalsElements { get; set; } = new List<object>();

        /// <summary>
        /// Lista de elementos que conforman el pie de página del ticket.
        /// </summary>
        public List<object> FooterElements { get; set; } = new List<object>();

        /// <summary>
        /// Lista de imágenes incrustadas en el ticket.
        /// </summary>
        public List<ImageProperties> Images { get; set; } = new List<ImageProperties>();

        /// <summary>
        /// Lista de códigos de barras incrustados en el ticket.
        /// </summary>
        public List<BarcodeProperties> Barcodes { get; set; } = new List<BarcodeProperties>();

        /// <summary>
        /// Lista de códigos QR incrustados en el ticket.
        /// </summary>
        public List<QRProperties> QRs { get; set; } = new List<QRProperties>();
    }
}
