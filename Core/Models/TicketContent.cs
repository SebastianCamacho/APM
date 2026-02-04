using System.Collections.Generic;

namespace AppsielPrintManager.Core.Models
{
    /// <summary>
    /// Representa el contenido estructurado de un ticket después de ser parseado
    /// y antes de ser convertido a comandos ESC/POS para la impresión.
    /// Ahora utiliza una estructura secuencial basada en secciones para permitir ordenamiento dinámico.
    /// </summary>
    public class TicketContent
    {
        /// <summary>
        /// El tipo de documento original para el que se generó este TicketContent.
        /// </summary>
        public string DocumentType { get; set; }

        /// <summary>
        /// Lista secuencial de todas las secciones renderizadas del ticket.
        /// El orden en esta lista es el orden exacto de impresión.
        /// </summary>
        public List<RenderedSection> Sections { get; set; } = new List<RenderedSection>();

        // Propiedades de compatibilidad para Media (Logo, QR manuales, etc.)
        // Estos se moverán a secciones dinámicas en el futuro, pero se mantienen por ahora.
        public List<RenderedElement> ExtraMedia { get; set; } = new List<RenderedElement>();
    }
}
