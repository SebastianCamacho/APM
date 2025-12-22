namespace AppsielPrintManager.Core.Models
{
    /// <summary>
    /// Representa las propiedades de un código QR a ser incluido en el ticket de impresión.
    /// </summary>
    public class QRProperties
    {
        /// <summary>
        /// El valor o datos a codificar en el código QR.
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// El tamaño aproximado del módulo QR en píxeles.
        /// </summary>
        public int Size { get; set; }

        /// <summary>
        /// La alineación del código QR en el ticket (ej. "center", "left", "right").
        /// </summary>
        public string Align { get; set; }
    }
}
