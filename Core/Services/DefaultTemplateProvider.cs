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
                "factura_electronica" => CreateDefaultElectronicInvoiceTemplate(),
                "sticker_codigo_barras" => CreateDefaultStickerTemplate(),
                _ => CreateDefaultTicketTemplate(documentType) // Por defecto un ticket estándar
            };
        }

        private static PrintTemplate CreateDefaultStickerTemplate()
        {
            return new PrintTemplate
            {
                TemplateId = null,
                DocumentType = "sticker_codigo_barras",
                Name = "Plantilla Etiquetas Código de Barras Predeterminada",
                Sections = new List<TemplateSection>
                {
                    new TemplateSection
                    {
                        Name = "Stickers",
                        Type = "Repeated",
                        DataSource = "stickers",
                        Align = "Center",
                        Order = 1,
                        Elements = new List<TemplateElement>
                        {
                            new TemplateElement
                            {
                                Type = "Barcode",
                                Label = null,
                                Source = "ItemId",
                                StaticValue = null,
                                Format = "",
                                Align = "Center",
                                HeaderFormat = null,
                                HeaderAlign = null,
                                WidthPercentage = null,
                                Columns = 1,
                                BarWidth = 2,
                                Height = 200,
                                Size = null,
                                Properties = new Dictionary<string, string>
                                {
                                    { "Hri", "true" }
                                }
                            }
                        }
                    }
                },
                GlobalStyles = new Dictionary<string, string>()
            };
        }

        private static PrintTemplate CreateDefaultElectronicInvoiceTemplate()
        {
            return new PrintTemplate
            {
                DocumentType = "factura_electronica",
                Name = "Plantilla Factura Electrónica Predeterminada",
                Sections = new List<TemplateSection>
                {
                    // Header: Seller Info & Logo
                    new TemplateSection
                    {
                        Name = "Header",
                        Type = "Static",
                        Align = "Center",
                        Order = 1,
                        Elements = new List<TemplateElement>
                        {
                            new TemplateElement { Type = "Text", Source = "Seller.Name", Format = "Bold Size2" },
                            new TemplateElement { Type = "Text", Source = "Seller.Nit", Label = "NIT: " },
                            new TemplateElement { Type = "Text", Source = "Seller.TaxRegime" },
                            new TemplateElement { Type = "Text", Source = "Seller.Address" },
                            new TemplateElement { Type = "Text", Source = "Seller.City" },
                            new TemplateElement { Type = "Text", Source = "Seller.Phone", Label = "Tel: " },
                             new TemplateElement { Type = "Text", Source = "Seller.ResolutionText", Format = "FontB" },
                            new TemplateElement { Type = "Line" }
                        }
                    },

                    // Buyer Info
                    new TemplateSection
                    {
                        Name = "Customer",
                        Type = "Static",
                        Align = "Left",
                        Order = 2,
                        Elements = new List<TemplateElement>
                        {
                            new TemplateElement { Type = "Text", Label = "Cliente: ", Source = "Buyer.Name", Format = "Bold" },
                            new TemplateElement { Type = "Text", Label = "NIT/CC: ", Source = "Buyer.Nit" },
                            new TemplateElement { Type = "Text", Label = "Dir: ", Source = "Buyer.Address" },
                             new TemplateElement { Type = "Text", Label = "Email: ", Source = "Buyer.Email" },
                            new TemplateElement { Type = "Line" }
                        }
                    },
                    // Invoice Details
                    new TemplateSection
                    {
                        Name = "Info",
                        Type = "Static",
                        Align = "Left",
                        Order = 3,
                        Elements = new List<TemplateElement>
                        {
                            new TemplateElement { Type = "Text", Source = "Invoice.Number", Label = "FACTURA DE VENTA N°: ", Format = "Bold" },
                            new TemplateElement { Type = "Text", Source = "Invoice.IssueDate", Label = "Fecha Emisión: " },
                            new TemplateElement { Type = "Text", Source = "Invoice.DueDate", Label = "Fecha Vencimiento: " },
                            new TemplateElement { Type = "Text", Source = "Invoice.PaymentMethod", Label = "Forma Pago: " },
                            new TemplateElement { Type = "Text", Source = "Invoice.PaymentMeans", Label = "Medio Pago: " },
                            new TemplateElement { Type = "Text", Source = "Invoice.Currency", Label = "Moneda: " },
                            new TemplateElement { Type = "Line" }
                        }
                    },
                    // Items Table
                    new TemplateSection
                    {
                        Name = "Items",
                        Type = "Table",
                        DataSource = "Invoice.Items",
                        Order = 4,
                        Elements = new List<TemplateElement>
                        {
                            new TemplateElement { Label = "Cant", Source = "Quantity", WidthPercentage = 10 },
                            new TemplateElement { Label = "Desc", Source = "Description", WidthPercentage = 40 },
                            new TemplateElement { Label = "Precio", Source = "UnitPrice", WidthPercentage = 19, Align = "Right" },
                            new TemplateElement { Label = "IVA", Source = "IvaRate", WidthPercentage = 10, Align = "Right" },
                            new TemplateElement { Label = "Total", Source = "Total", WidthPercentage = 21, Align = "Right" }
                        }
                    },
                    // Totals
                    new TemplateSection
                    {
                        Name = "Totals",
                        Type = "Static",
                        Order = 5,
                        Align = "Right",
                        Elements = new List<TemplateElement>
                        {
                            new TemplateElement { Type = "Line" },
                            new TemplateElement { Type = "Text", Source = "Invoice.Subtotal", Label = "Subtotal: " },
                            new TemplateElement { Type = "Text", Source = "Invoice.Discount", Label = "Descuento: " },
                            new TemplateElement { Type = "Text", Source = "Invoice.Iva", Label = "Total IVA: " },
                            new TemplateElement { Type = "Text", Source = "Invoice.Total", Label = "TOTAL A PAGAR: ", Format = "Bold Size2" },
                            new TemplateElement { Type = "Line" }
                        }
                    },

                    // QR & Legal (Resolución, CUFE)
                    new TemplateSection
                    {
                        Name = "Footer",
                        Type = "Static",
                        Order = 6,
                        Align = "Center",
                        Elements = new List<TemplateElement>
                        {
                            new TemplateElement { Type = "Text", Label = "CUFE: ", Source = "TechKey", Format = "FontB" },
                            new TemplateElement { Type = "QR", Source = "QrString", Properties = new Dictionary<string, string> { { "Size", "8" } } },
                            new TemplateElement { Type = "Text", StaticValue = "Factura generada por Software APPSIEL POS" }
                        }
                    }
                }
            };
        }

        private static PrintTemplate CreateDefaultTicketTemplate(string type)
        {
            return new PrintTemplate
            {
                DocumentType = type,
                Name = "Plantilla Ticket de Venta Predeterminada",
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
                            new TemplateElement { Type = "Text", Source = "sale.Total", Label = "TOTAL: ", Format = "Bold FontA Size2" },
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
                Name = "Plantilla de Comanda Predeterminada",
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
                                Label = "Descripción",
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
