namespace AppsielPrintManager.Core.Models
{
    public class RenderedElement
    {
        public string Type { get; set; } // e.g., "Text", "Image", "Barcode", "QR"
        public string TextValue { get; set; } // For text elements
        public string Base64Image { get; set; } // For images
        public string BarcodeValue { get; set; } // For barcodes
        public string BarcodeType { get; set; } // Type of barcode (e.g., "CODE128")
        public string QrValue { get; set; } // For QR
        public string Format { get; set; } // Processed formats (e.g., "Bold|Center")
        public string Align { get; set; } // Processed alignment
        public int? WidthMm { get; set; } // For images
        public int? Height { get; set; } // For barcodes
        public bool? Hri { get; set; } // For barcodes
        public int? Size { get; set; } // For QR
        public int? WidthPercentage { get; set; } // For dynamic table column width

        // Propiedades para Barcode extendido
        public string? ItemId { get; set; }
        public string? ProductName { get; set; }
        public string? ProductPrice { get; set; }

        // Add any other properties that might be needed after rendering and before ESC/POS generation
    }
}
