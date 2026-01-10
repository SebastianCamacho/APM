using System;
using System.Collections.Generic;

namespace AppsielPrintManager.Core.Models
{
    /// <summary>
    /// Representa los datos específicos para un documento de tipo "ticket_venta".
    /// </summary>
    public class SaleTicketDocumentData
    {
        public CompanyInfo Company { get; set; }
        public SaleInfo Sale { get; set; }
        public List<string> Footer { get; set; } = new List<string>();

        /// <summary>
        /// Representa la información de la compañía en un trabajo de impresión.
        /// Anidada para ser específica de este documento.
        /// </summary>
        public class CompanyInfo
        {
            public string Name { get; set; }
            public string Nit { get; set; }
            public string Address { get; set; }
            public string Phone { get; set; }
        }

        /// <summary>
        /// Representa la información detallada de una venta para un trabajo de impresión.
        /// Anidada para ser específica de este documento.
        /// </summary>
        public class SaleInfo
        {
            public string Number { get; set; }
            public DateTime Date { get; set; }
            public List<SaleItem> Items { get; set; } = new List<SaleItem>();
            public decimal Subtotal { get; set; }
            public decimal Tax { get; set; }
            public decimal Total { get; set; }
        }

        /// <summary>
        /// Representa un ítem o producto individual dentro de una venta.
        /// Anidada para ser específica de este documento.
        /// </summary>
        public class SaleItem
        {
            public string Name { get; set; }
            public int Qty { get; set; }
            public decimal UnitPrice { get; set; }
            public decimal Total { get; set; }
        }
    }
}
