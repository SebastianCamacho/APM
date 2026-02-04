using System.Collections.Generic;

namespace AppsielPrintManager.Core.Models
{
    /// <summary>
    /// Representa una sección ya procesada y lista para ser enviada al generador ESC/POS.
    /// </summary>
    public class RenderedSection
    {
        public string? Name { get; set; }
        public string? Type { get; set; } // "Static", "Table"

        // Para secciones estáticas (lista simple de elementos)
        public List<RenderedElement> Elements { get; set; } = new List<RenderedElement>();

        // Para secciones de tabla (lista de filas, donde cada fila es una lista de elementos)
        public List<List<RenderedElement>> TableRows { get; set; } = new List<List<RenderedElement>>();
    }
}
