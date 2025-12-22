using System;
using System.Collections.Generic;

namespace AppsielPrintManager.Core.Models
{
    /// <summary>
    /// Representa la información detallada de una venta para un trabajo de impresión.
    /// </summary>
    public class SaleInfo
    {
        /// <summary>
        /// Número de identificación de la venta.
        /// </summary>
        public string Number { get; set; }

        /// <summary>
        /// Fecha y hora de la venta.
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Subtotal de la venta antes de impuestos.
        /// </summary>
        public decimal Subtotal { get; set; }

        /// <summary>
        /// Monto total de impuestos aplicados a la venta.
        /// </summary>
        public decimal Tax { get; set; }

        /// <summary>
        /// Monto total de la venta (subtotal + impuestos).
        /// </summary>
        public decimal Total { get; set; }

        /// <summary>
        /// Lista de ítems o productos incluidos en la venta.
        /// </summary>
        public List<SaleItem> Items { get; set; }
    }
}
