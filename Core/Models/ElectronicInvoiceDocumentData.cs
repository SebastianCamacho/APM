using System;
using System.Collections.Generic;

namespace AppsielPrintManager.Core.Models
{
    /// <summary>
    /// Representa los datos específicos para un documento de tipo "factura_electronica".
    /// </summary>
    /// <summary>
    /// Representa los datos específicos para un documento de tipo "factura_electronica".
    /// </summary>
    public class ElectronicInvoiceDocumentData
    {
        public SellerInfo? Seller { get; set; }
        public BuyerInfo? Buyer { get; set; }
        public InvoiceInfo? Invoice { get; set; }
        public string? TechKey { get; set; } // CUFE o similar
        public string? QrString { get; set; } // Cadena cruda del QR
        public List<string> LegalNotes { get; set; } = new List<string>();

        /// <summary>
        /// Información del Emisor (Vendedor). Similar a CompanyInfo en Ticket.
        /// </summary>
        public class SellerInfo
        {
            public string? Name { get; set; }
            public string? Nit { get; set; }
            public string? TaxRegime { get; set; } // Ej: "Responsable de IVA"
            public string? Address { get; set; }
            public string? City { get; set; } // Ciudad / País
            public string? Phone { get; set; }
            public string? Email { get; set; }

            // Datos de Resolución DIAN
            public string? ResolutionNumber { get; set; }
            public DateTime? ResolutionDate { get; set; }
            public string? ResolutionPrefix { get; set; }
            public string? ResolutionFrom { get; set; } // String para flexibilidad ("1" o "001")
            public string? ResolutionTo { get; set; }

            public string? ResolutionText { get; set; } // "Autorización DIAN..." (Texto completo opcional)
        }

        /// <summary>
        /// Información del Adquirente (Comprador).
        /// </summary>
        public class BuyerInfo
        {
            public string? Name { get; set; }
            public string? Nit { get; set; }
            public string? Address { get; set; }
            public string? Email { get; set; }
        }

        /// <summary>
        /// Información detallada de la factura (Similar a SaleInfo).
        /// </summary>
        public class InvoiceInfo
        {
            public string? Number { get; set; } // Prefijo + Consecutivo
            public DateTime IssueDate { get; set; }
            public DateTime DueDate { get; set; }
            public string? PaymentMethod { get; set; } // "Contado", "Crédito"
            public string? PaymentMeans { get; set; } // "Efectivo", "Tarjeta", "Transferencia"
            public string? Currency { get; set; } // "COP"

            public List<InvoiceLine> Items { get; set; } = new List<InvoiceLine>();

            public decimal Subtotal { get; set; }
            public decimal Discount { get; set; }
            public decimal Iva { get; set; } // Total de IVA
            public decimal Total { get; set; }

            // Desglose de impuestos por tipo (IVA 19%, INC 8%, etc.)
            public List<TaxDetail> Taxes { get; set; } = new List<TaxDetail>();
        }

        public class InvoiceLine
        {
            public string? Code { get; set; }
            public string? Description { get; set; }
            public decimal Quantity { get; set; }
            public decimal UnitPrice { get; set; }
            public decimal Discount { get; set; }
            public decimal IvaRate { get; set; } // % IVA
            public decimal IvaAmount { get; set; }
            public decimal Total { get; set; }
        }

        public class TaxDetail
        {
            public string? Name { get; set; } // "IVA 19%"
            public decimal Base { get; set; }
            public decimal Rate { get; set; }
            public decimal Amount { get; set; }
        }
    }
}
