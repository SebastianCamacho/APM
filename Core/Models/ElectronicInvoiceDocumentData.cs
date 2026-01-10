using System;
using System.Collections.Generic;

namespace AppsielPrintManager.Core.Models
{
    /// <summary>
    /// Representa los datos específicos para un documento de tipo "factura_electronica".
    /// </summary>
    public class ElectronicInvoiceDocumentData
    {
        public HeaderInfo Header { get; set; }
        public CustomerInfo Customer { get; set; }
        public TotalsInfo Totals { get; set; }
        public string Cufe { get; set; }

        /// <summary>
        /// Información del encabezado de la factura.
        /// </summary>
        public class HeaderInfo
        {
            public string Title { get; set; }
            public string Number { get; set; }
        }

        /// <summary>
        /// Información del cliente de la factura.
        /// </summary>
        public class CustomerInfo
        {
            public string Name { get; set; }
            public string Nit { get; set; }
            public string Address { get; set; }
        }

        /// <summary>
        /// Información de totales de la factura.
        /// </summary>
        public class TotalsInfo
        {
            public decimal Subtotal { get; set; }
            public decimal Tax { get; set; }
            public decimal Total { get; set; }
        }
    }
}
