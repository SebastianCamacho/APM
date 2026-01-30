using AppsielPrintManager.Core.Models;
using System.Collections.Generic;

namespace AppsielPrintManager.Core.Services
{
    public static class DefaultTemplateProvider
    {
        public static PrintTemplate GetDefaultTemplate(string documentType)
        {
            return documentType.ToLower() switch
            {
                "comanda" => CreateDefaultCommandTemplate(),
                _ => CreateDefaultTicketTemplate(documentType) // Por defecto un ticket estándar
            };
        }

        private static PrintTemplate CreateDefaultTicketTemplate(string type)
        {
            return new PrintTemplate
            {
                DocumentType = type,
                Name = "Plantilla Estándar",
                Sections = new List<TemplateSection>
                {
                    new TemplateSection
                    {
                        Name = "Header",
                        Type = "Static",
                        Align = "Center",
                        Order = 1,
                        Elements = new List<TemplateElement>
                        {
                            new TemplateElement { Type = "Text", StaticValue = "APPSIEL CLOUD POS", Format = "Bold Size2" },
                            new TemplateElement { Type = "Text", Source = "company.Name", Format = "Bold" },
                            new TemplateElement { Type = "Text", Source = "company.Address" },
                            new TemplateElement { Type = "Text", Source = "company.Phone", Label = "Tel: " },
                            new TemplateElement { Type = "Line" }
                        }
                    },
                    new TemplateSection
                    {
                        Name = "Info",
                        Type = "Static",
                        Order = 2,
                        Elements = new List<TemplateElement>
                        {
                            new TemplateElement { Type = "Text", Source = "sale.Number", Label = "Ticket #: ", Format = "Bold" },
                            new TemplateElement { Type = "Text", Source = "sale.Date", Label = "Fecha: " },
                            new TemplateElement { Type = "Line" }
                        }
                    },
                    new TemplateSection
                    {
                        Name = "Items",
                        Type = "Table",
                        DataSource = "sale.Items",
                        Order = 3,
                        Elements = new List<TemplateElement>
                        {
                            new TemplateElement { Label = "Cant", Source = "Qty", WidthPercentage = 13 },
                            new TemplateElement { Label = "Producto", Source = "Name", WidthPercentage = 55 },
                            new TemplateElement { Label = "Total", Source = "Total", WidthPercentage = 17, Align = "Right" },
                            new TemplateElement { Label = "Total", Source = "Total", WidthPercentage = 15, Align = "Right" }
                        }
                    },
                    new TemplateSection
                    {
                        Name = "Totals",
                        Type = "Static",
                        Order = 4,
                        Align = "Right",
                        Elements = new List<TemplateElement>
                        {
                            new TemplateElement { Type = "Line" },
                            new TemplateElement { Type = "Text", Source = "sale.Subtotal", Label = "Subtotal: " },
                            new TemplateElement { Type = "Text", Source = "sale.IVA", Label = "IVA: " },
                            new TemplateElement { Type = "Text", Source = "sale.Total", Label = "TOTAL: ", Format = "Bold FontB Size2" },
                            new TemplateElement { Type = "Line" }
                        }
                    },
                    new TemplateSection
                    {
                        Name = "Footer",
                        Type = "Static",
                        Align = "Center",
                        Order = 5,
                        Elements = new List<TemplateElement>
                        {
                            new TemplateElement { Type = "Text", StaticValue = "¡GRACIAS POR SU COMPRA!", Format = "Bold" },
                            new TemplateElement { Type = "QR", Source = "sale.InvoiceUrl", Properties = new Dictionary<string, string> { { "Size", "4" } } }
                        }
                    }
                }
            };
        }

        private static PrintTemplate CreateDefaultCommandTemplate()
        {
            return new PrintTemplate
            {
                TemplateId = "comanda-default",
                Name = "Plantilla Predeterminada de Comanda",
                DocumentType = "comanda",
                Sections = new List<TemplateSection>
                {
                    new TemplateSection
                    {
                        Name = "Header",
                        Type = "Static",
                        Order = 1,
                        Align = "Center",
                        Elements = new List<TemplateElement>
                        {
                            new TemplateElement { Type = "Text", Source = "order.COPY", Label = "COPY:", Format = "Bold", Align = "Left" },
                            new TemplateElement { Type = "Text", Source = "order.RestaurantName", Format = "Bold,Large" },
                            new TemplateElement { Type = "Text", Label = "COMANDA N°: ", Source = "order.Number", Format = "Bold" },
                            new TemplateElement { Type = "Text", Label = "Mesa: ", Source = "order.Table" },
                            new TemplateElement { Type = "Text", Label = "Mesero: ", Source = "order.Waiter" },
                            new TemplateElement { Type = "Text", Label = "Fecha: ", Source = "order.Date" },
                            new TemplateElement { Type = "Line" }
                        }
                    },
                    new TemplateSection
                    {
                        Name = "Items",
                        Type = "Table",
                        Format = "FontA Size3",
                        DataSource = "order.Items",
                        Order = 2,
                        Elements = new List<TemplateElement>
                        {
                            new TemplateElement
                            {
                                Label = "Cant",
                                Source = "Qty",
                                WidthPercentage = 25,
                                HeaderFormat = "FontB Size2",
                                Format = "FontA Size3 ",
                                Align = "Left"
                            },
                            new TemplateElement
                            {
                                Label = "Cant",
                                Source = "Name",
                                WidthPercentage = 75,
                                HeaderFormat = "FontB Size2",
                                Format = "FontA Size3 ",
                                Align = "Left"
                            }
                        }
                    },
                    new TemplateSection
                    {
                        Name = "Footer",
                        Type = "Static",
                        Order = 3,
                        Align = "Center",
                        Elements = new List<TemplateElement>
                        {
                            new TemplateElement { Type = "Line" },
                            new TemplateElement { Type = "Text", Label = "Notas: ", Source = "Detail",Format = "FontB Size2 " },
                            new TemplateElement { Type = "Text", Source = "order.GeneratedDate" },
                            new TemplateElement { Type = "Text", StaticValue = "Impreso por APM ", Format = "" }
                        }
                    }
                }
            };
        }
    }
}
