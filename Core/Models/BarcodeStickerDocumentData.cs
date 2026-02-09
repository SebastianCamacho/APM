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
            public string ItemId { get; set; }
            public string Name { get; set; }
            public string Price { get; set; }
            public string Value { get; set; } // El valor del código de barras
            public string Type { get; set; } // Ej. "EAN13", "CODE128"
            public int? Height { get; set; }
            public int? Width { get; set; } // Ancho del módulo (1-6)
            public bool? Hri { get; set; }
        }
    }
}
