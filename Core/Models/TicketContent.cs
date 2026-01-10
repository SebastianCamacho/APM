using System.Collections.Generic;

namespace AppsielPrintManager.Core.Models
{
    /// <summary>
    /// Representa el contenido estructurado de un ticket después de ser parseado
    /// y antes de ser convertido a comandos ESC/POS para la impresión.
    /// Este modelo abstrae la disposición visual del ticket y utiliza elementos ya renderizados.
    /// </summary>
    public class TicketContent
    {
        /// <summary>
        /// El tipo de documento original para el que se generó este TicketContent.
        /// Útil para la lógica de renderizado específica del generador ESC/POS.
        /// </summary>
        public string DocumentType { get; set; }

        /// <summary>
        /// Lista de elementos ya renderizados que conforman la sección de detalles (ej. para comandas).
        /// </summary>
        public List<RenderedElement> Details { get; set; } = new List<RenderedElement>();
        /// <summary>
        /// Lista de elementos ya renderizados que conforman el encabezado del ticket.
        /// </summary>
        public List<RenderedElement> HeaderElements { get; set; } = new List<RenderedElement>();

        /// <summary>
        /// Lista de filas de la tabla de ítems del ticket, cada fila contiene elementos ya renderizados.
        /// </summary>
        public List<List<RenderedElement>> ItemTableRows { get; set; } = new List<List<RenderedElement>>();

        /// <summary>
        /// Lista de elementos ya renderizados que muestran los totales de la venta.
        /// </summary>
        public List<RenderedElement> TotalsElements { get; set; } = new List<RenderedElement>();

        /// <summary>
        /// Lista de elementos ya renderizados que conforman el pie de página del ticket.
        /// </summary>
        public List<RenderedElement> FooterElements { get; set; } = new List<RenderedElement>();

        /// <summary>
        /// Lista de filas de contenido repetitivo (ej. stickers de códigos de barras),
        /// donde cada fila puede contener múltiples elementos ya renderizados.
        /// </summary>
        public List<List<RenderedElement>> RepeatedContent { get; set; } = new List<List<RenderedElement>>();

        /// <summary>
        /// Indica cuántos elementos se deben mostrar por fila en el RepeatedContent.
        /// Útil para la disposición de stickers (ej. 1 o 2 por fila).
        /// </summary>
        public int RepeatedContentColumns { get; set; } = 1;
    }
}

