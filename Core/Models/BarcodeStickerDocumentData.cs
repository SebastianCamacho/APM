using System.Collections.Generic;

namespace AppsielPrintManager.Core.Models
{
    /// <summary>
    /// Representa los datos específicos para un documento de tipo "sticker_codigo_barras".
    /// </summary>
    public class BarcodeStickerDocumentData
    {
        public List<StickerData> Stickers { get; set; } = new List<StickerData>();

        /// <summary>
        /// Representa los datos para un sticker individual de código de barras.
        /// Anidada para ser específica de este documento.
        /// </summary>
        public class StickerData
        {
            public string ProductName { get; set; }
            public string ProductCode { get; set; }
            public string BarcodeValue { get; set; }
            public string BarcodeType { get; set; } // Ej. "EAN13", "CODE128"
        }
    }
}
