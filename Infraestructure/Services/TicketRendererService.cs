using AppsielPrintManager.Core.Interfaces;
using AppsielPrintManager.Core.Models;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq; // Necesario para Any()
using System.Text.Json; // Necesario para JsonSerializer, JsonElement

namespace AppsielPrintManager.Infraestructure.Services
{
    /// <summary>
    /// Implementación de ITicketRenderer.
    /// Es responsable de tomar los datos de un PrintJobRequest y transformarlos
    /// en una estructura de TicketContent que representa la disposición visual del ticket.
    /// </summary>
    public class TicketRendererService : ITicketRenderer
    {
        private readonly ILoggingService _logger;

        public TicketRendererService(ILoggingService logger)
        {
            _logger = logger;
        }

        public Task<TicketContent> RenderTicketAsync(PrintJobRequest request)
        {
            _logger.LogInfo($"Iniciando renderizado de ticket para JobId: {request.JobId} (DocumentType: {request.DocumentType})");

            var ticketContent = new TicketContent();
            var jsonSerializerOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            // 1. Mover directamente las propiedades de multimedia
            if (request.Images != null) ticketContent.Images.AddRange(request.Images);
            if (request.Barcodes != null) ticketContent.Barcodes.AddRange(request.Barcodes);
            if (request.QRs != null) ticketContent.QRs.AddRange(request.QRs);

            // 2. Procesar el campo 'Data' que es un Dictionary<string, object>
            (CompanyInfo companyInfo, SaleInfo saleInfo, CommandInfo commandInfo, List<string> footerLines) = 
                ProcessRequestData(request, jsonSerializerOptions);
            
            // 3. Construir elementos del ticket según DocumentType
            switch (request.DocumentType)
            {
                case "ticket_venta":
                    BuildSaleTicketContent(request, ticketContent, companyInfo, saleInfo, footerLines);
                    break;
                case "comanda":
                    BuildCommandTicketContent(request, ticketContent, commandInfo, footerLines);
                    break;
                default:
                    _logger.LogWarning($"DocumentType '{request.DocumentType}' no reconocido. Renderizando con datos básicos.");
                    ticketContent.HeaderElements.Add($"Documento no reconocido: {request.DocumentType}");
                    ticketContent.HeaderElements.Add($"Job ID: {request.JobId}");
                    ticketContent.FooterElements.Add("Por favor, contacte a soporte.");
                    break;
            }

            _logger.LogInfo($"Renderizado de ticket completado para JobId: {request.JobId}");
            return Task.FromResult(ticketContent);
        }

        /// <summary>
        /// Procesa el diccionario Data del PrintJobRequest para extraer la información relevante
        /// según el DocumentType.
        /// </summary>
        private (CompanyInfo companyInfo, SaleInfo saleInfo, CommandInfo commandInfo, List<string> footerLines) ProcessRequestData(PrintJobRequest request, JsonSerializerOptions jsonSerializerOptions)
        {
            CompanyInfo companyInfo = null; 
            SaleInfo saleInfo = null;
            CommandInfo commandInfo = null;
            List<string> footerLines = new List<string>();

            if (request.Data == null) return (null, null, null, new List<string>());

            // Intentar extraer Footer (común a todos los tipos si está presente)
            if (request.Data.TryGetValue("footer", out object footerObj) && footerObj != null)
            {
                try
                {
                    var jsonElement = (JsonElement)footerObj;
                    footerLines = JsonSerializer.Deserialize<List<string>>(jsonElement, jsonSerializerOptions);
                    _logger.LogInfo($"Footer extraído con {footerLines?.Count} líneas.");
                }
                catch (System.Exception ex)
                {
                    _logger.LogError($"Error al deserializar Footer: {ex.Message}", ex);
                }
            }

            if (request.DocumentType == "ticket_venta")
            {
                if (request.Data.TryGetValue("company", out object companyObj) && companyObj != null)
                {
                    try
                    {
                        var jsonElement = (JsonElement)companyObj;
                        companyInfo = JsonSerializer.Deserialize<CompanyInfo>(jsonElement, jsonSerializerOptions);
                        _logger.LogInfo($"CompanyInfo extraída: {companyInfo?.Name}");
                    }
                    catch (System.Exception ex)
                    {
                        _logger.LogError($"Error al deserializar CompanyInfo: {ex.Message}", ex);
                    }
                }

                if (request.Data.TryGetValue("sale", out object saleObj) && saleObj != null)
                {
                    try
                    {
                        var jsonElement = (JsonElement)saleObj;
                        saleInfo = JsonSerializer.Deserialize<SaleInfo>(jsonElement, jsonSerializerOptions);
                        _logger.LogInfo($"SaleInfo extraída para venta: {saleInfo?.Number}");
                    }
                    catch (System.Exception ex)
                    {
                        _logger.LogError($"Error al deserializar SaleInfo: {ex.Message}", ex);
                    }
                }
            }
            else if (request.DocumentType == "comanda")
            {
                if (request.Data.TryGetValue("command", out object commandObj) && commandObj != null)
                {
                    try
                    {
                        var jsonElement = (JsonElement)commandObj;
                        commandInfo = JsonSerializer.Deserialize<CommandInfo>(jsonElement, jsonSerializerOptions);
                        _logger.LogInfo($"CommandInfo extraída para pedido: {commandInfo?.OrderNumber}");
                    }
                    catch (System.Exception ex)
                    {
                        _logger.LogError($"Error al deserializar CommandInfo: {ex.Message}", ex);
                    }
                }
            }
            return (companyInfo, saleInfo, commandInfo, footerLines);
        }

        /// <summary>
        /// Construye el contenido del ticket para un DocumentType "ticket_venta".
        /// </summary>
        private void BuildSaleTicketContent(PrintJobRequest request, TicketContent ticketContent, 
                                            CompanyInfo companyInfo, SaleInfo saleInfo, List<string> footerLines)
        {
            if (companyInfo != null)
            {
                ticketContent.HeaderElements.Add(companyInfo.Name);
                ticketContent.HeaderElements.Add($"NIT: {companyInfo.Nit}");
                ticketContent.HeaderElements.Add(companyInfo.Address);
                ticketContent.HeaderElements.Add($"Tel: {companyInfo.Phone}");
            } else {
                ticketContent.HeaderElements.Add("--- Sin Información de Empresa ---");
            }
            ticketContent.HeaderElements.Add($"Documento: {request.DocumentType}");
            ticketContent.HeaderElements.Add($"Job ID: {request.JobId}");

            if (saleInfo?.Items != null && saleInfo.Items.Any())
            {
                ticketContent.ItemTableRows.Add(new List<object> { "Producto", "Cant.", "V.Unit", "Total" });
                foreach (var item in saleInfo.Items)
                {
                    ticketContent.ItemTableRows.Add(new List<object> { item.Name, item.Qty, item.UnitPrice.ToString("N2"), item.Total.ToString("N2") });
                }
            } else {
                 ticketContent.ItemTableRows.Add(new List<object> { "--- No hay ítems en la venta ---" });
            }

            if (saleInfo != null)
            {
                ticketContent.TotalsElements.Add($"Subtotal: {saleInfo.Subtotal:N2}");
                ticketContent.TotalsElements.Add($"IVA: {saleInfo.Tax:N2}");
                ticketContent.TotalsElements.Add($"TOTAL: {saleInfo.Total:N2}");
            }

            if (footerLines.Any())
            {
                ticketContent.FooterElements.AddRange(footerLines);
            } else {
                ticketContent.FooterElements.Add("¡Gracias por su compra!");
            }
        }

        /// <summary>
        /// Construye el contenido del ticket para un DocumentType "comanda".
        /// </summary>
        private void BuildCommandTicketContent(PrintJobRequest request, TicketContent ticketContent, 
                                               CommandInfo commandInfo, List<string> footerLines)
        {
            if (commandInfo != null)
            {
                ticketContent.HeaderElements.Add(commandInfo.RestaurantName ?? "Restaurante");
                ticketContent.HeaderElements.Add($"Pedido No. {commandInfo.OrderNumber}");
                ticketContent.HeaderElements.Add($"Fecha: {commandInfo.Date:dd MMM yyyy}");
                ticketContent.HeaderElements.Add($"Cliente: {commandInfo.Client}");
                ticketContent.HeaderElements.Add($"Atendido por: {commandInfo.AttendedBy}");
            } else {
                ticketContent.HeaderElements.Add("--- Sin Información de Comanda ---");
            }
            ticketContent.HeaderElements.Add($"Documento: {request.DocumentType}");
            
            if (commandInfo?.Items != null && commandInfo.Items.Any())
            {
                ticketContent.ItemTableRows.Add(new List<object> { "Producto", "Cant." });
                foreach (var item in commandInfo.Items)
                {
                    ticketContent.ItemTableRows.Add(new List<object> { item.Name, item.Qty });
                }
            } else {
                ticketContent.ItemTableRows.Add(new List<object> { "--- No hay ítems en la comanda ---" });
            }

            if (!string.IsNullOrWhiteSpace(commandInfo?.Detail))
            {
                ticketContent.FooterElements.Add($"Detalle: {commandInfo.Detail}");
            }
            if (commandInfo != null)
            {
                     ticketContent.FooterElements.Add($"Generado: {commandInfo.GeneratedDate:yyyy-MM-dd HH:mm:ss}");
                     ticketContent.FooterElements.Add($"Creado por: {commandInfo.CreatedBy}");
            }
            if (footerLines.Any()) // Añadir footerlines si se proporcionaron globalmente
            {
                ticketContent.FooterElements.AddRange(footerLines);
            } else {
                ticketContent.FooterElements.Add(new string('-', 32)); // Separador final
                ticketContent.FooterElements.Add("¡Gracias por su preferencia!");
            }
        }
    }
}