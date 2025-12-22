using System.Collections.Generic;

namespace AppsielPrintManager.Core.Models
{
    /// <summary>
    /// Representa la solicitud completa de un trabajo de impresión enviada por Appsiel.
    /// Este modelo encapsula todos los datos necesarios para generar un ticket.
    /// </summary>
    public class PrintJobRequest
    {
        /// <summary>
        /// Identificador único del trabajo de impresión.
        /// </summary>
        public string JobId { get; set; }

        /// <summary>
        /// Identificador de la estación o caja desde donde se origina la solicitud.
        /// </summary>
        public string StationId { get; set; }

        /// <summary>
        /// Identificador de la impresora destino donde se debe realizar la impresión.
        /// </summary>
        public string PrinterId { get; set; }

        /// <summary>
        /// Tipo de documento a imprimir (ej. "ticket_venta", "comanda", "factura_electronica", "sticker").
        /// Este tipo define el formato de impresión a utilizar.
        /// </summary>
        public string DocumentType { get; set; }

        /// <summary>
        /// Contiene los datos principales del documento a imprimir.
        /// Es un diccionario flexible para acomodar diferentes estructuras según el DocumentType.
        /// Puede contener, por ejemplo, información de empresa (CompanyInfo) o venta (SaleInfo).
        /// </summary>
        public Dictionary<string, object> Data { get; set; }

        /// <summary>
        /// Lista de imágenes a insertar en el ticket, cada una con sus propiedades.
        /// </summary>
        public List<ImageProperties> Images { get; set; }

        /// <summary>
        /// Lista de códigos de barras a insertar en el ticket, cada uno con sus propiedades.
        /// </summary>
        public List<BarcodeProperties> Barcodes { get; set; }

        /// <summary>
        /// Lista de códigos QR a insertar en el ticket, cada uno con sus propiedades.
        /// </summary>
        public List<QRProperties> QRs { get; set; }
    }
}