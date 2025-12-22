namespace AppsielPrintManager.Core.Models
{
    /// <summary>
    /// Representa la configuración guardada para una impresora POS térmica.
    /// </summary>
    public class PrinterSettings
    {
        /// <summary>
        /// Identificador único de la impresora, usado por Appsiel para mapear trabajos.
        /// </summary>
        public string PrinterId { get; set; }

        /// <summary>
        /// Dirección IP fija de la impresora en la red local.
        /// </summary>
        public string IpAddress { get; set; }

        /// <summary>
        /// Puerto TCP/IP de la impresora (normalmente 9100).
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Ancho del papel de la impresora en milímetros (ej. 58mm o 80mm).
        /// </summary>
        public int PaperWidthMm { get; set; }

        /// <summary>
        /// Indica si la impresora debe emitir un sonido (beep) al imprimir.
        /// </summary>
        public bool BeepOnPrint { get; set; }

        /// <summary>
        /// Indica si la impresora debe abrir el cajón monedero después de imprimir.
        /// </summary>
        public bool OpenCashDrawerAfterPrint { get; set; }

        /// <summary>
        /// Indica si la impresora debe abrir el cajón monedero sin imprimir.
        /// </summary>
        public bool OpenCashDrawerWithoutPrint { get; set; }

        /// <summary>
        /// Lista de identificadores de impresoras adicionales a las que se debe copiar el trabajo de impresión.
        /// Cada elemento en la lista es un 'PrinterId' de otra impresora configurada.
        /// </summary>
        public List<string> CopyToPrinterIds { get; set; } = new List<string>();
    }
}
