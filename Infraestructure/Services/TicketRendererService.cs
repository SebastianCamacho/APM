using AppsielPrintManager.Core.Interfaces;
using AppsielPrintManager.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace AppsielPrintManager.Infraestructure.Services
{
    /// <summary>
    /// Implementación de ITicketRenderer.
    /// Es responsable de tomar los datos de un PrintJobRequest y transformarlos
    /// en una estructura de TicketContent que representa la disposición visual del ticket
    /// utilizando plantillas predefinidas.
    /// </summary>
    public class TicketRendererService : ITicketRenderer
    {
        private readonly ILoggingService _logger;
        public TicketRendererService(ILoggingService logger)
        {
            _logger = logger;
        }



        public Task<TicketContent> RenderTicketAsync(PrintJobRequest request, object documentData)
        {
            _logger.LogInfo($"Iniciando renderizado de ticket para JobId: {request.JobId} (DocumentType: {request.DocumentType})");

            var ticketContent = new TicketContent();

            switch (request.DocumentType)
            {
                case "ticket_venta":
                    if (documentData is SaleTicketDocumentData saleData)
                    {
                        return RenderSaleTicket(request, saleData);
                    }
                    else
                    {
                        _logger.LogError($"Tipo de datos incorrecto para 'ticket_venta'. Se esperaba SaleTicketDocumentData.");
                        ticketContent.HeaderElements.Add(new RenderedElement { Type = "Text", TextValue = "ERROR: Datos de venta incorrectos.", Format = "Bold", Align = "Center" });
                    }
                    break;
                case "comanda":
                    if (documentData is CommandDocumentData commandData)
                    {
                        return RenderCommandDocument(request, commandData);
                    }
                    else
                    {
                        _logger.LogError($"Tipo de datos incorrecto para 'comanda'. Se esperaba CommandDocumentData.");
                        ticketContent.HeaderElements.Add(new RenderedElement { Type = "Text", TextValue = "ERROR: Datos de comanda incorrectos.", Format = "Bold", Align = "Center" });
                    }
                    break;
                case "factura_electronica":
                    if (documentData is ElectronicInvoiceDocumentData invoiceData)
                    {
                        return RenderElectronicInvoiceDocument(request, invoiceData);
                    }
                    else
                    {
                        _logger.LogError($"Tipo de datos incorrecto para 'factura_electronica'. Se esperaba ElectronicInvoiceDocumentData.");
                        ticketContent.HeaderElements.Add(new RenderedElement { Type = "Text", TextValue = "ERROR: Datos de factura incorrectos.", Format = "Bold", Align = "Center" });
                    }
                    break;
                case "sticker_codigo_barras":
                    if (documentData is BarcodeStickerDocumentData stickerData)
                    {
                        return RenderBarcodeStickerDocument(request, stickerData);
                    }
                    else
                    {
                        _logger.LogError($"Tipo de datos incorrecto para 'sticker_codigo_barras'. Se esperaba BarcodeStickerDocumentData.");
                        ticketContent.HeaderElements.Add(new RenderedElement { Type = "Text", TextValue = "ERROR: Datos de sticker incorrectos.", Format = "Bold", Align = "Center" });
                    }
                    break;
                default:
                    _logger.LogError($"DocumentType '{request.DocumentType}' no soportado.");
                    ticketContent.HeaderElements.Add(new RenderedElement { Type = "Text", TextValue = $"ERROR: Tipo de documento '{request.DocumentType}' no soportado.", Format = "Bold", Align = "Center" });
                    break;
            }

            _logger.LogInfo($"Renderizado de ticket finalizado para JobId: {request.JobId}.");
            return Task.FromResult(ticketContent);
        }


        // Métodos de renderizado específicos por tipo de documento
        private Task<TicketContent> RenderSaleTicket(PrintJobRequest request, SaleTicketDocumentData data)
        {
            var ticketContent = new TicketContent();
            ticketContent.DocumentType = request.DocumentType;

            // HEADER
            ticketContent.HeaderElements.Add(new RenderedElement { Type = "Text", TextValue = data.Company.Name, Format = "Bold", Align = "Center" });
            ticketContent.HeaderElements.Add(new RenderedElement { Type = "Text", TextValue = $"NIT: {data.Company.Nit}", Align = "Center" });
            ticketContent.HeaderElements.Add(new RenderedElement { Type = "Text", TextValue = data.Company.Address, Align = "Center" });
            ticketContent.HeaderElements.Add(new RenderedElement { Type = "Text", TextValue = data.Company.Phone, Align = "Center" });
            ticketContent.HeaderElements.Add(new RenderedElement { Type = "Text", TextValue = "--------------------------------", Align = "Center" });
            ticketContent.HeaderElements.Add(new RenderedElement { Type = "Text", TextValue = "TICKET DE VENTA", Format = "Bold", Align = "Center" });
            ticketContent.HeaderElements.Add(new RenderedElement { Type = "Text", TextValue = $"Fecha: {data.Sale.Date:yyyy-MM-dd HH:mm:ss}", Align = "Center" });
            ticketContent.HeaderElements.Add(new RenderedElement { Type = "Text", TextValue = $"No. Venta: {data.Sale.Number}", Align = "Center" });
            ticketContent.HeaderElements.Add(new RenderedElement { Type = "Text", TextValue = "--------------------------------", Align = "Center" });

            // ITEMS
            ticketContent.ItemTableRows.Add(new List<RenderedElement>
            {
                new RenderedElement { Type = "Text", TextValue = "Producto", Format = "Bold" },
                new RenderedElement { Type = "Text", TextValue = "Cant.", Format = "Bold" },
                new RenderedElement { Type = "Text", TextValue = "P.Unitario", Format = "Bold" },
                new RenderedElement { Type = "Text", TextValue = "Total", Format = "Bold" }
            });
            foreach (var item in data.Sale.Items)
            {
                ticketContent.ItemTableRows.Add(new List<RenderedElement>
                {
                    new RenderedElement { Type = "Text", TextValue = item.Name },
                    new RenderedElement { Type = "Text", TextValue = item.Qty.ToString() },
                    new RenderedElement { Type = "Text", TextValue = item.UnitPrice.ToString("N2") }, // Añadido P.Unitario
                    new RenderedElement { Type = "Text", TextValue = item.Total.ToString("N2") }
                });
            }

            // TOTALS
            ticketContent.TotalsElements.Add(new RenderedElement { Type = "Text", TextValue = $"Subtotal: {data.Sale.Subtotal:N2}", Align = "Right" });
            ticketContent.TotalsElements.Add(new RenderedElement { Type = "Text", TextValue = $"IVA: {data.Sale.Tax:N2}", Align = "Right" });
            ticketContent.TotalsElements.Add(new RenderedElement { Type = "Text", TextValue = $"TOTAL: {data.Sale.Total:N2}", Format = "Bold", Align = "Right" });

            // FOOTER
            foreach (var footerLine in data.Footer)
            {
                ticketContent.FooterElements.Add(new RenderedElement { Type = "Text", TextValue = footerLine, Align = "Center" });
            }
            ticketContent.FooterElements.Add(new RenderedElement { Type = "Text", TextValue = "Gracias por su compra!", Align = "Center" });

            AddPrintJobMediaToTicketContent(request, ticketContent);
            return Task.FromResult(ticketContent);
        }

        private Task<TicketContent> RenderCommandDocument(PrintJobRequest request, CommandDocumentData data)
        {
            var ticketContent = new TicketContent();
            ticketContent.DocumentType = request.DocumentType;

            // HEADER
            ticketContent.HeaderElements.Add(new RenderedElement { Type = "Text", TextValue = "COMANDA DE PEDIDO", Format = "Bold", Align = "Center" });
            ticketContent.HeaderElements.Add(new RenderedElement { Type = "Text", TextValue = $"Pedido No. {data.Order.Number}", Align = "Left" });
            ticketContent.HeaderElements.Add(new RenderedElement { Type = "Text", TextValue = $"Fecha: {data.Order.Date:dd MMM yyyy}", Align = "Left" });
            ticketContent.HeaderElements.Add(new RenderedElement { Type = "Text", TextValue = $"Cliente: {data.Order.Table}", Align = "Left" }); // Mapeamos Table a Cliente
            ticketContent.HeaderElements.Add(new RenderedElement { Type = "Text", TextValue = $"Atendido por: {data.Order.Waiter}", Align = "Left" }); // Mapeamos Waiter a Atendido por

            // ITEMS TABLE
            ticketContent.ItemTableRows.Add(new List<RenderedElement>
            {
                new RenderedElement { Type = "Text", TextValue = "Producto", Format = "Bold" },
                new RenderedElement { Type = "Text", TextValue = "Cant", Format = "Bold" },
                new RenderedElement { Type = "Text", TextValue = "Notas", Format = "Bold" } // Mantener Notes para que EscPosGeneratorService la procese
            });
            foreach (var item in data.Order.Items)
            {
                ticketContent.ItemTableRows.Add(new List<RenderedElement>
                {
                    new RenderedElement { Type = "Text", TextValue = item.Name },
                    new RenderedElement { Type = "Text", TextValue = item.Qty.ToString() },
                    new RenderedElement { Type = "Text", TextValue = item.Notes }
                });
            }

            // DETALLE
            if (!string.IsNullOrEmpty(data.Detail))
            {
                ticketContent.Details.Add(new RenderedElement { Type = "Text", TextValue = "Detalle:", Align = "Left", Format = "Bold" });
                ticketContent.Details.Add(new RenderedElement { Type = "Text", TextValue = data.Detail, Align = "Left" });
            }

            // FOOTER
            ticketContent.FooterElements.Add(new RenderedElement { Type = "Text", TextValue = $"{data.Order.RestaurantName}", Align = "Left" });
            ticketContent.FooterElements.Add(new RenderedElement { Type = "Text", TextValue = $"Generado: {data.Order.GeneratedDate:yyyy-MM-dd HH:mm:ss}", Align = "Left" });
            ticketContent.FooterElements.Add(new RenderedElement { Type = "Text", TextValue = $"Creado por: {data.Order.CreatedBy}", Align = "Left" });

            AddPrintJobMediaToTicketContent(request, ticketContent);
            return Task.FromResult(ticketContent);
        }

        private Task<TicketContent> RenderElectronicInvoiceDocument(PrintJobRequest request, ElectronicInvoiceDocumentData data)
        {
            var ticketContent = new TicketContent();
            ticketContent.DocumentType = request.DocumentType;

            // HEADER
            ticketContent.HeaderElements.Add(new RenderedElement { Type = "Text", TextValue = data.Header.Title, Format = "Bold", Align = "Center" });
            ticketContent.HeaderElements.Add(new RenderedElement { Type = "Text", TextValue = $"No. {data.Header.Number}", Align = "Center" });
            ticketContent.HeaderElements.Add(new RenderedElement { Type = "Text", TextValue = "--------------------------------", Align = "Center" });
            ticketContent.HeaderElements.Add(new RenderedElement { Type = "Text", TextValue = "CLIENTE", Format = "Bold", Align = "Left" });
            ticketContent.HeaderElements.Add(new RenderedElement { Type = "Text", TextValue = $"Nombre: {data.Customer.Name}", Align = "Left" });
            ticketContent.HeaderElements.Add(new RenderedElement { Type = "Text", TextValue = $"NIT: {data.Customer.Nit}", Align = "Left" });
            ticketContent.HeaderElements.Add(new RenderedElement { Type = "Text", TextValue = $"Dirección: {data.Customer.Address}", Align = "Left" });
            ticketContent.HeaderElements.Add(new RenderedElement { Type = "Text", TextValue = "--------------------------------", Align = "Center" });

            // TOTALS (para factura los totales suelen ir después de los ítems, pero como no tenemos ítems aquí, los pondremos directos)
            ticketContent.TotalsElements.Add(new RenderedElement { Type = "Text", TextValue = $"Subtotal: {data.Totals.Subtotal:N2}", Align = "Right" });
            ticketContent.TotalsElements.Add(new RenderedElement { Type = "Text", TextValue = $"IVA: {data.Totals.Tax:N2}", Align = "Right" });
            ticketContent.TotalsElements.Add(new RenderedElement { Type = "Text", TextValue = $"TOTAL: {data.Totals.Total:N2}", Format = "Bold", Align = "Right" });
            ticketContent.TotalsElements.Add(new RenderedElement { Type = "Text", TextValue = "--------------------------------", Align = "Center" });


            // FOOTER
            ticketContent.FooterElements.Add(new RenderedElement { Type = "Text", TextValue = $"CUFE: {data.Cufe}", Align = "Center", Format = "Small" }); // Asumiendo que CUFE podría ser un texto más pequeño

            AddPrintJobMediaToTicketContent(request, ticketContent);
            return Task.FromResult(ticketContent);
        }

        private Task<TicketContent> RenderBarcodeStickerDocument(PrintJobRequest request, BarcodeStickerDocumentData data)
        {
            var ticketContent = new TicketContent();
            ticketContent.DocumentType = request.DocumentType;

            // La lógica de stickers es diferente, va en RepeatedContent
            ticketContent.RepeatedContentColumns = 1; // Por defecto 1, el generador de ESC/POS decidirá si puede poner más.

            foreach (var sticker in data.Stickers)
            {
                var stickerElements = new List<RenderedElement>
                {
                    new RenderedElement { Type = "Text", TextValue = sticker.ProductName, Align = "Center", Format = "Bold" },
                    new RenderedElement { Type = "Text", TextValue = sticker.ProductCode, Align = "Center" },
                    new RenderedElement { Type = "Barcode", BarcodeValue = sticker.BarcodeValue, BarcodeType = sticker.BarcodeType, Height = 50, Hri = true, Align = "Center" },
                    new RenderedElement { Type = "Text", TextValue = " ", Align = "Center" } // Espacio extra entre stickers
                };
                ticketContent.RepeatedContent.Add(stickerElements);
                ticketContent.RepeatedContent.Add(new List<RenderedElement> { new RenderedElement { Type = "Text", TextValue = "----------", Align = "Center" } }); // Separador entre stickers
            }
            
            AddPrintJobMediaToTicketContent(request, ticketContent);
            return Task.FromResult(ticketContent);
        }

        private void AddPrintJobMediaToTicketContent(PrintJobRequest request, TicketContent ticketContent)
        {
            // Añadir imágenes si existen
            if (request.Images != null && request.Images.Any())
            {
                foreach (var img in request.Images)
                {
                    ticketContent.FooterElements.Add(new RenderedElement
                    {
                        Type = "Image",
                        Base64Image = img.Base64,
                        Align = img.Align,
                        WidthMm = img.WidthMm
                    });
                }
            }

            // Añadir códigos de barras si existen
            if (request.Barcodes != null && request.Barcodes.Any())
            {
                foreach (var bc in request.Barcodes)
                {
                    ticketContent.FooterElements.Add(new RenderedElement
                    {
                        Type = "Barcode",
                        BarcodeValue = bc.Value,
                        BarcodeType = bc.Type,
                        Align = bc.Align,
                        Height = bc.Height,
                        Hri = bc.Hri
                    });
                }
            }

            // Añadir códigos QR si existen
            if (request.QRs != null && request.QRs.Any())
            {
                foreach (var qr in request.QRs)
                {
                    ticketContent.FooterElements.Add(new RenderedElement
                    {
                        Type = "QR",
                        QrValue = qr.Value,
                        Align = qr.Align,
                        Size = qr.Size
                    });
                }
            }
        }
    }
}