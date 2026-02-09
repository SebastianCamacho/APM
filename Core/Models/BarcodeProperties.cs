namespace AppsielPrintManager.Core.Models
{
    /// <summary>
    /// Representa las propiedades de un código de barras a ser incluido en el ticket de impresión.
    /// </summary>
    public class BarcodeProperties
    {
        /// <summary>
        /// Tipo de código de barras (ej. "CODE128", "EAN13", "UPC-A").
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// El valor o datos a codificar en el código de barras.
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// La altura deseada del código de barras.
        /// </summary>
        public int Height { get; set; }
        public int? Width { get; set; } // Ancho del módulo

        /// <summary>
        /// Indica si se debe imprimir la interpretación legible por humanos (Human Readable Interpretation - HRI)
        /// debajo del código de barras.
        /// </summary>
        public bool Hri { get; set; }

        /// <summary>
        /// La alineación del código de barras en el ticket (ej. "center", "left", "right").
        /// </summary>

        public string ItemId { get; set; }
        public string Name { get; set; }
        public string Price { get; set; }
    }
}
