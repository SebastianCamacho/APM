using System.Collections.Generic;

namespace AppsielPrintManager.Core.Models
{
    /// <summary>
    /// Representa la configuración completa de una plantilla de impresión.
    /// </summary>
    public class PrintTemplate
    {
        public string? TemplateId { get; set; }
        public string? Name { get; set; }
        public string? DocumentType { get; set; }
        public List<TemplateSection> Sections { get; set; } = new List<TemplateSection>();
        public Dictionary<string, string> GlobalStyles { get; set; } = new Dictionary<string, string>();
    }

    /// <summary>
    /// Representa una sección lógica dentro del ticket (ej: Header, Items, Totals, Footer).
    /// </summary>
    public class TemplateSection
    {
        public string? Name { get; set; }
        public string? Type { get; set; } // "Static", "Table", "Repeated"
        public string? DataSource { get; set; } // Ruta al objeto de datos
        public string? Format { get; set; } // Formato global para la sección
        public string? Align { get; set; } // Alineación global para la sección
        public int? Order { get; set; } // Orden de impresión de la sección
        public List<TemplateElement> Elements { get; set; } = new List<TemplateElement>();
    }

    /// <summary>
    /// Define un elemento individual o una columna dentro de una sección.
    /// </summary>
    public class TemplateElement
    {
        public string? Type { get; set; } // "Text", "Barcode", "QR", "Image", "Line"
        public string? Label { get; set; } // Tekto estático antes del valor
        public string? Source { get; set; } // Ruta de la propiedad del modelo de datos (ej: "Sale.Number")
        public string? StaticValue { get; set; } // Valor fijo si Source es nulo
        public string? Format { get; set; } // "Bold", "Large", "Italic", etc.
        public string? Align { get; set; } // "Left", "Center", "Right"
        public string? HeaderFormat { get; set; } // Nuevo: Formato específico para el encabezado
        public string? HeaderAlign { get; set; } // Nuevo: Alineación específica para el encabezado
        public int? WidthPercentage { get; set; } // Para columnas de tabla
        public int? BarWidth { get; set; } // Ancho del módulo de código de barras (1-5)
        public int? Height { get; set; } // Altura del código de barras o imagen (1-255)
        public int? Size { get; set; } // Tamaño para QR (1-16) u otros elementos
        public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();
    }
}
