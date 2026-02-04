using System;
using System.Collections.Generic;

namespace AppsielPrintManager.Core.Models
{
    /// <summary>
    /// Representa los datos específicos para un documento de tipo "comanda".
    /// </summary>
    public class CommandDocumentData
    {
        public CommandInfo Order { get; set; }
        public string Detail { get; set; }

        /// <summary>
        /// Representa la información detallada de una comanda de cocina/bar.
        /// Anidada para ser específica de este documento, ajustada al main.js de prueba.
        /// </summary>
        public class CommandInfo
        {
            public string COPY { get; set; }
            public string Number { get; set; }
            public string Table { get; set; }
            public string Waiter { get; set; }
            public DateTime Date { get; set; }
            public string RestaurantName { get; set; }
            public DateTime GeneratedDate { get; set; }
            public string CreatedBy { get; set; }
            public List<CommandItem> Items { get; set; } = new List<CommandItem>();
        }

        /// <summary>
        /// Representa un ítem o producto individual dentro de una comanda.
        /// Anidada para ser específica de este documento.
        /// </summary>
        public class CommandItem
        {
            public string Name { get; set; }
            public int Qty { get; set; }
            public string Notes { get; set; } // Restaurado
        }
    }
}
