using System.Collections.Generic;

namespace AppsielPrintManager.Core.Services
{
    public static class TemplateCatalogService
    {
        public static List<string> GetDataSourceSuggestions(string? documentType = null)
        {
            var list = new List<string>();
            bool isComanda = documentType?.ToLower() == "comanda";

            if (isComanda)
            {
                list.Add("order.Items");
            }
            else
            {
                list.Add("sale.Items");
                list.Add("footer");
            }

            list.Add("barcode.Items");
            return list;
        }

        public static List<string> GetGlobalSourceSuggestions(string? documentType = null)
        {
            var list = new List<string>();
            bool isComanda = documentType?.ToLower() == "comanda";

            if (isComanda)
            {
                list.AddRange(new[] {
                    "order.Number", "order.Table", "order.Waiter", "order.Date",
                    "order.RestaurantName", "order.COPY", "order.GeneratedDate",
                    "order.CreatedBy", "Detail"
                });
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
            var list = new List<string> { "Name", "Qty" };
            bool isComanda = documentType?.ToLower() == "comanda";

            if (isComanda)
            {
                list.Add("Notes");
            }
            else
            {
                list.AddRange(new[] { "UnitPrice", "Total" });
            }

            // Sugerencias adicionales comunes
            list.AddRange(new[] { "Description", "Code", "Price" });

            return list;
        }
    }
}
