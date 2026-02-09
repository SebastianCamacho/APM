using System.Collections.Generic;

namespace AppsielPrintManager.Core.Services
{
    public static class TemplateCatalogService
    {
        public static List<string> GetDataSourceSuggestions(string? documentType = null)
        {
            var list = new List<string>();
            var type = documentType?.ToLower();

            if (type == "sticker_codigo_barras")
            {
                list.Add("stickers");
                return list;
            }

            if (type == "comanda")
            {
                list.Add("order.Items");
            }
            else if (type == "factura_electronica")
            {
                list.Add("Invoice.Items");
                list.Add("Invoice.Taxes");
                list.Add("LegalNotes");
            }
            else
            {
                list.Add("sale.Items");
                list.Add("footer");
                list.Add("barcode.Items");
            }

            return list;
        }

        public static List<string> GetGlobalSourceSuggestions(string? documentType = null)
        {
            var list = new List<string>();
            var type = documentType?.ToLower();

            if (type == "sticker_codigo_barras")
            {
                // Los stickers generalmente no tienen datos globales complejos, 
                // pero si se enviaran datos de empresa se podrian agregar aqui.
                // Por ahora retornamos vacio para no confundir.
                return list;
            }

            if (type == "comanda")
            {
                list.AddRange(new[] {
                    "order.Number", "order.Table", "order.Waiter", "order.Date",
                    "order.RestaurantName", "order.COPY", "order.GeneratedDate",
                    "order.CreatedBy", "Detail"
                });
            }
            else if (type == "factura_electronica")
            {
                // Seller
                list.AddRange(new[] {
                    "Seller.Name", "Seller.Nit", "Seller.TaxRegime", "Seller.Address",
                    "Seller.City", "Seller.Phone", "Seller.Email",
                    "Seller.ResolutionNumber", "Seller.ResolutionText"
                });

                // Buyer
                list.AddRange(new[] {
                    "Buyer.Name", "Buyer.Nit", "Buyer.Address", "Buyer.Email", "Buyer.Phone"
                });

                // Invoice
                list.AddRange(new[] {
                    "Invoice.Number", "Invoice.IssueDate", "Invoice.DueDate", "Invoice.PaymentMethod",
                    "Invoice.PaymentMeans", "Invoice.Currency",
                    "Invoice.Subtotal", "Invoice.Discount", "Invoice.Iva", "Invoice.Total"
                });

                // Other
                list.AddRange(new[] { "TechKey", "QrString" });
            }
            else
            {
                list.AddRange(new[] {
                    "sale.Number", "sale.Date", "sale.Subtotal", "sale.IVA",
                    "sale.Total", "footer"
                });

                // Para tickets, incluimos company
                list.AddRange(new[] {
                    "company.Name", "company.Nit", "company.Address", "company.Phone"
                });
            }

            return list;
        }

        public static List<string> GetItemSourceSuggestions(string? documentType = null)
        {
            var type = documentType?.ToLower();

            if (type == "sticker_codigo_barras")
            {
                // Para stickers, retornamos SOLO las propiedades relevantes y salimos
                return new List<string> { "ItemId", "Name", "Price", "Value", "Height", "Width", "Hri" };
            }

            // Lista base para otros documentos
            var list = new List<string> { "Quantity", "Description" };

            if (type == "comanda")
            {
                list.Add("Notes");
                list.Add("Name"); // Comandas usa Name
            }
            else if (type == "factura_electronica")
            {
                list.AddRange(new[] { "UnitPrice", "IvaRate", "IvaAmount", "Total" });
            }
            else
            {
                // Ticket venta standard
                list.AddRange(new[] { "Name", "Qty", "UnitPrice", "Total" });
            }

            // Sugerencias adicionales comunes (No agregar a stickers)
            list.AddRange(new[] { "Code", "Price" });

            return list;
        }
    }
}
