namespace AppsielPrintManager.Core.Models
{
    /// <summary>
    /// Representa las propiedades de una imagen a ser incluida en el ticket de impresión.
    /// </summary>
    public class ImageProperties
    {
        /// <summary>
        /// Identificador único para la imagen, si es necesario.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// La imagen codificada en formato Base64.
        /// </summary>
        public string Base64 { get; set; }

        /// <summary>
        /// La alineación de la imagen en el ticket (ej. "center", "left", "right").
        /// </summary>
        public string Align { get; set; }

        /// <summary>
        /// El ancho deseado de la imagen en milímetros.
        /// </summary>
        public int WidthMm { get; set; }
    }
}
