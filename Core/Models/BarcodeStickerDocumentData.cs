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
            public string item_id { get; set; }
            public string name { get; set; }
            public string price { get; set; }
            public string value { get; set; } // El valor del código de barras
            public string type { get; set; } // Ej. "EAN13", "CODE128"
            public int? height { get; set; }
            public bool? hri { get; set; }
        }
    }
}
