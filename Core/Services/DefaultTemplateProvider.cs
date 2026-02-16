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
                                Order = 1,
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
                            new TemplateElement { Type = "Text", Source = "Seller.Name", Format = "Bold Size2", Order = 1 },
                            new TemplateElement { Type = "Text", Source = "Seller.Nit", Label = "NIT: ", Order = 2 },
                            new TemplateElement { Type = "Text", Source = "Seller.TaxRegime", Order = 3 },
                            new TemplateElement { Type = "Text", Source = "Seller.Address", Order = 4 },
                            new TemplateElement { Type = "Text", Source = "Seller.City", Order = 5 },
                            new TemplateElement { Type = "Text", Source = "Seller.Phone", Label = "Tel: ", Order = 6 },
                             new TemplateElement { Type = "Text", Source = "Seller.ResolutionText", Format = "FontB", Order = 7 },
                            new TemplateElement { Type = "Line", Order = 8 }
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
                            new TemplateElement { Type = "Text", Label = "Cliente: ", Source = "Buyer.Name", Format = "Bold", Order = 1 },
                            new TemplateElement { Type = "Text", Label = "NIT/CC: ", Source = "Buyer.Nit", Order = 2 },
                            new TemplateElement { Type = "Text", Label = "Dir: ", Source = "Buyer.Address", Order = 3 },
                             new TemplateElement { Type = "Text", Label = "Email: ", Source = "Buyer.Email", Order = 4 },
                            new TemplateElement { Type = "Line", Order = 5 }
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
                            new TemplateElement { Type = "Text", Source = "Invoice.Number", Label = "FACTURA DE VENTA N°: ", Format = "Bold", Order = 1 },
                            new TemplateElement { Type = "Text", Source = "Invoice.IssueDate", Label = "Fecha Emisión: ", Order = 2 },
                            new TemplateElement { Type = "Text", Source = "Invoice.DueDate", Label = "Fecha Vencimiento: ", Order = 3 },
                            new TemplateElement { Type = "Text", Source = "Invoice.PaymentMethod", Label = "Forma Pago: ", Order = 4 },
                            new TemplateElement { Type = "Text", Source = "Invoice.PaymentMeans", Label = "Medio Pago: ", Order = 5 },
                            new TemplateElement { Type = "Text", Source = "Invoice.Currency", Label = "Moneda: ", Order = 6 },
                            new TemplateElement { Type = "Line", Order = 7 }
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
                            new TemplateElement { Label = "Cant", Source = "Quantity", WidthPercentage = 10, Order = 1 },
                            new TemplateElement { Label = "Desc", Source = "Description", WidthPercentage = 40, Order = 2 },
                            new TemplateElement { Label = "Precio", Source = "UnitPrice", WidthPercentage = 19, Align = "Right", Order = 3 },
                            new TemplateElement { Label = "IVA", Source = "IvaRate", WidthPercentage = 10, Align = "Right", Order = 4 },
                            new TemplateElement { Label = "Total", Source = "Total", WidthPercentage = 21, Align = "Right", Order = 5 }
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
                            new TemplateElement { Type = "Line", Order = 1 },
                            new TemplateElement { Type = "Text", Source = "Invoice.Subtotal", Label = "Subtotal: ", Order = 2 },
                            new TemplateElement { Type = "Text", Source = "Invoice.Discount", Label = "Descuento: ", Order = 3 },
                            new TemplateElement { Type = "Text", Source = "Invoice.Iva", Label = "Total IVA: ", Order = 4 },
                            new TemplateElement { Type = "Text", Source = "Invoice.Total", Label = "TOTAL A PAGAR: ", Format = "Bold Size2", Order = 5 },
                            new TemplateElement { Type = "Line", Order = 6 }
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
                            new TemplateElement { Type = "Text", Label = "CUFE: ", Source = "TechKey", Format = "FontB", Order = 1 },
                            new TemplateElement { Type = "QR", Source = "QrString", Order = 2, Properties = new Dictionary<string, string> { { "Size", "8" } } },
                            new TemplateElement { Type = "Text", StaticValue = "Factura generada por Software APPSIEL POS", Order = 3 }
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
                            new TemplateElement { Type = "Text", StaticValue = "APPSIEL CLOUD POS", Format = "Bold Size2", Order = 1 },
                            new TemplateElement { Type = "Text", Source = "company.Name", Format = "Bold", Order = 2 },
                            new TemplateElement { Type = "Text", Source = "company.Address", Order = 3 },
                            new TemplateElement { Type = "Text", Source = "company.Phone", Label = "Tel: ", Order = 4 },
                            new TemplateElement { Type = "Line", Order = 5 }
                        }
                    },
                    new TemplateSection
                    {
                        Name = "Info",
                        Type = "Static",
                        Order = 2,
                        Elements = new List<TemplateElement>
                        {
                            new TemplateElement { Type = "Text", Source = "sale.Number", Label = "Ticket #: ", Format = "Bold", Order = 1 },
                            new TemplateElement { Type = "Text", Source = "sale.Date", Label = "Fecha: ", Order = 2 },
                            new TemplateElement { Type = "Line", Order = 3 }
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
                            new TemplateElement { Label = "Cant", Source = "Qty", WidthPercentage = 13, Order = 1 },
                            new TemplateElement { Label = "Producto", Source = "Name", WidthPercentage = 55, Order = 2 },
                            new TemplateElement { Label = "Total", Source = "Total", WidthPercentage = 17, Align = "Right", Order = 3 },
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
                            new TemplateElement { Type = "Line", Order = 1 },
                            new TemplateElement { Type = "Text", Source = "sale.Subtotal", Label = "Subtotal: ", Order = 2 },
                            new TemplateElement { Type = "Text", Source = "sale.IVA", Label = "IVA: ", Order = 3 },
                            new TemplateElement { Type = "Text", Source = "sale.Total", Label = "TOTAL: ", Format = "Bold FontA Size2", Order = 4 },
                            new TemplateElement { Type = "Line", Order = 5 }
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
                            new TemplateElement { Type = "Text", StaticValue = "¡GRACIAS POR SU COMPRA!", Format = "Bold", Order = 1 },
                            new TemplateElement { Type = "QR", Source = "sale.InvoiceUrl", Order = 2, Properties = new Dictionary<string, string> { { "Size", "4" } } }
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
                            new TemplateElement { Type = "Text", Source = "order.COPY", Label = "COPY:", Format = "Bold", Align = "Left", Order = 1 },
                            new TemplateElement { Type = "Text", Source = "order.RestaurantName", Format = "Bold,Large", Order = 2 },
                            new TemplateElement { Type = "Text", Label = "COMANDA N°: ", Source = "order.Number", Format = "Bold", Order = 3 },
                            new TemplateElement { Type = "Text", Label = "Mesa: ", Source = "order.Table", Order = 4 },
                            new TemplateElement { Type = "Text", Label = "Mesero: ", Source = "order.Waiter", Order = 5 },
                            new TemplateElement { Type = "Text", Label = "Fecha: ", Source = "order.Date", Order = 6 },
                            new TemplateElement { Type = "Line", Order = 7 }
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
                                Align = "Left",
                                Order = 1
                            },
                            new TemplateElement
                            {
                                Label = "Descripción",
                                Source = "Name",
                                WidthPercentage = 75,
                                HeaderFormat = "FontB Size2",
                                Format = "FontA Size3 ",
                                Align = "Left",
                                Order = 2
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
                            new TemplateElement { Type = "Line", Order = 1 },
                            new TemplateElement { Type = "Text", Label = "Notas: ", Source = "Detail",Format = "FontB Size2 ", Order = 2 },
                            new TemplateElement { Type = "Text", Source = "order.GeneratedDate", Order = 3 },
                            new TemplateElement { Type = "Text", StaticValue = "Impreso por APM ", Format = "", Order = 4 }
                        }
                    }
                }
            };
        }
    }
}
