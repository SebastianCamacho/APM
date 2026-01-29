using System.Collections.Generic;

namespace AppsielPrintManager.Core.Services
{
    public static class TemplateCatalogService
    {
        public static List<string> GetDataSourceSuggestions()
        {
            return new List<string>
            {
                "Order.Items",
                "Sale.Items",
                "Barcode.Items"
            };
        }

        public static List<string> GetGlobalSourceSuggestions()
        {
            return new List<string>
            {
                "Order.Number",
                "Order.Table",
                "Order.Waiter",
                "Order.Date",
                "Order.RestaurantName",
                "Order.COPY",
                "Order.GeneratedDate",
                "Order.CreatedBy",
                "Company.Name",
                "Company.Nit",
                "Company.Address",
                "Company.Phone",
                "Sale.Number",
                "Sale.Date",
                "Sale.Subtotal",
                "Sale.IVA",
                "Sale.Total",
                "Detail"
            };
        }

        public static List<string> GetItemSourceSuggestions()
        {
            return new List<string>
            {
                "Name",
                "Qty",
                "Notes",
                "UnitPrice",
                "Total",
                "Description",
                "Code",
                "Price"
            };
        }
    }
}
