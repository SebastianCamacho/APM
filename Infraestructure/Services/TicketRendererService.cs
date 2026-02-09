using AppsielPrintManager.Core.Interfaces;
using AppsielPrintManager.Core.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace AppsielPrintManager.Infraestructure.Services
{
    public class TicketRendererService : ITicketRenderer
    {
        private readonly ILoggingService _logger;
        private readonly ITemplateRepository _templateRepository;

        // Diccionario para normalizar tipos conocidos y evitar problemas de Case-Sensitivity
        private static readonly Dictionary<string, string> _knownTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            { "QR", "QR" },
            { "BARCODE", "Barcode" },
            { "IMAGE", "Image" },
            { "TEXT", "Text" },
            { "LINE", "Line" }
        };

        public TicketRendererService(ILoggingService logger, ITemplateRepository templateRepository)
        {
            _logger = logger;
            _templateRepository = templateRepository;
        }

        public async Task<TicketContent> RenderTicketAsync(PrintJobRequest request, object documentData)
        {
            _logger.LogInfo($"Iniciando renderizado dinámico para JobId: {request.JobId} ({request.DocumentType})");

            var template = await _templateRepository.GetTemplateByTypeAsync(request.DocumentType);

            if (template == null)
            {
                _logger.LogWarning($"No se encontró plantilla para '{request.DocumentType}'. Usando renderizado básico de error.");
                return CreateErrorTicket(request.JobId, $"Falta plantilla para {request.DocumentType}");
            }

            var ticketContent = new TicketContent { DocumentType = request.DocumentType };

            // 1. Ordenar secciones
            var orderedSections = template.Sections
                .OrderBy(s => s.Order ?? 999)
                .ThenBy(s => template.Sections.IndexOf(s)); // Mantener orden original si no hay Order

            // 2. Procesar secuencialmente
            foreach (var section in orderedSections)
            {
                try
                {
                    var renderedSection = await ProcessSection(section, documentData);
                    if (renderedSection != null)
                    {
                        ticketContent.Sections.Add(renderedSection);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error al procesar sección '{section.Name}': {ex.Message}", ex);
                }
            }

            // Añadir media del request original al final (por compatibilidad)
            AddPrintJobMediaToTicketContent(request, ticketContent);

            return ticketContent;
        }

        private async Task<RenderedSection?> ProcessSection(TemplateSection section, object data)
        {
            var renderedSection = new RenderedSection { Name = section.Name, Type = section.Type ?? "Static" };

            switch (section.Type?.ToLower())
            {
                case "table":
                    var listData = GetValueFromPath(data, section.DataSource ?? string.Empty) as IEnumerable;
                    if (listData != null)
                    {
                        // Encabezados de tabla: Heredan de la sección si no hay HeaderFormat
                        var headers = section.Elements.Select(e => new RenderedElement
                        {
                            Type = "Text",
                            TextValue = e.Label ?? string.Empty,
                            Format = e.HeaderFormat ?? section.Format,
                            Align = e.HeaderAlign ?? e.Align ?? section.Align ?? "Left",
                            WidthPercentage = e.WidthPercentage
                        }).ToList();

                        renderedSection.TableRows.Add(headers);

                        // Filas de datos: Heredan de e.Format o section.Format
                        foreach (var item in listData)
                        {
                            var row = section.Elements.Select(e => new RenderedElement
                            {
                                Type = "Text",
                                TextValue = GetValueFromPath(item, e.Source ?? string.Empty)?.ToString() ?? string.Empty,
                                Align = e.Align ?? section.Align ?? "Left",
                                Format = e.Format ?? section.Format,
                                WidthPercentage = e.WidthPercentage
                            }).ToList();
                            renderedSection.TableRows.Add(row);
                        }
                    }
                    break;

                case "repeated":
                    var repeatedData = GetValueFromPath(data, section.DataSource ?? string.Empty) as IEnumerable;
                    if (repeatedData != null)
                    {
                        foreach (var item in repeatedData)
                        {
                            foreach (var element in section.Elements)
                            {
                                // Soporte para "." como origen (item actual en la lista)
                                var val = !string.IsNullOrEmpty(element.Source) && element.Source != "."
                                    ? GetValueFromPath(item, element.Source!)?.ToString()
                                    : (element.Source == "." ? item?.ToString() : element.StaticValue);

                                string rawType = element.Type ?? "Text";
                                string normalizedType = _knownTypes.TryGetValue(rawType, out var known) ? known : rawType;

                                var textValue = $"{(element.Label ?? string.Empty)}{val}";

                                var renderedElement = new RenderedElement
                                {
                                    Type = normalizedType,
                                    TextValue = textValue,
                                    Align = element.Align ?? section.Align,
                                    Format = element.Format ?? section.Format
                                };

                                if (normalizedType == "Barcode")
                                {
                                    renderedElement.BarcodeValue = val;

                                    // 1. Obtener nombres de campos desde el diccionario de Propiedades (Default to PascalCase)
                                    string nameField = (element.Properties != null && element.Properties.ContainsKey("NameSource")) ? element.Properties["NameSource"] : "Name";
                                    string idField = (element.Properties != null && element.Properties.ContainsKey("ItemIdSource")) ? element.Properties["ItemIdSource"] : "ItemId";
                                    string priceField = (element.Properties != null && element.Properties.ContainsKey("PriceSource")) ? element.Properties["PriceSource"] : "Price";

                                    // 2. Extraer valores STRICT MODE: Solo lo que diga la plantilla
                                    renderedElement.ProductName = GetValueFromPath(item, nameField)?.ToString();
                                    renderedElement.ItemId = GetValueFromPath(item, idField)?.ToString();
                                    renderedElement.ProductPrice = GetValueFromPath(item, priceField)?.ToString();

                                    // DEBUG LOGS
                                    _logger.LogInfo($"[TicketRenderer] Barcode processing. Item: {JsonSerializer.Serialize(item)}");
                                    _logger.LogInfo($"[TicketRenderer] Extracted - Name ({nameField}): {renderedElement.ProductName}, Id ({idField}): {renderedElement.ItemId}, Price ({priceField}): {renderedElement.ProductPrice}");



                                    // Propiedades HRI y Height (Prioridad: Datos > Plantilla)
                                    // 1. Intentar obtener de los datos
                                    int? dataHeight = GetValueFromPath(item, "Height") as int? ?? (int.TryParse(GetValueFromPath(item, "Height")?.ToString(), out int h) ? h : null);

                                    // 2. Si no hay dato, usar plantilla
                                    if (dataHeight.HasValue)
                                        renderedElement.Height = dataHeight;
                                    else if (element.Properties != null && element.Properties.ContainsKey("Height"))
                                        renderedElement.Height = int.Parse(element.Properties["Height"]);


                                    // 1. Intentar obtener de los datos
                                    bool? dataHri = GetValueFromPath(item, "Hri") as bool? ?? (bool.TryParse(GetValueFromPath(item, "Hri")?.ToString(), out bool b) ? b : null);

                                    // 2. Si no hay dato, usar plantilla
                                    if (dataHri.HasValue)
                                        renderedElement.Hri = dataHri;
                                    else if (element.Properties != null && element.Properties.ContainsKey("Hri"))
                                        renderedElement.Hri = bool.Parse(element.Properties["Hri"]);


                                    // Propiedad WIDTH (Ancho del módulo)
                                    // 1. Intentar obtener de los datos
                                    int? dataWidth = GetValueFromPath(item, "Width") as int? ?? (int.TryParse(GetValueFromPath(item, "Width")?.ToString(), out int w) ? w : null);

                                    // 2. Si no hay dato, usar plantilla
                                    if (dataWidth.HasValue)
                                        renderedElement.BarWidth = dataWidth;
                                    else if (element.Properties != null && element.Properties.ContainsKey("Width"))
                                        renderedElement.BarWidth = int.Parse(element.Properties["Width"]);

                                }
                                else if (normalizedType == "QR")
                                {
                                    renderedElement.QrValue = val;
                                    if (element.Properties != null && element.Properties.ContainsKey("Size"))
                                    {
                                        int.TryParse(element.Properties["Size"], out int size);
                                        renderedElement.Size = size;
                                    }
                                }

                                renderedSection.Elements.Add(renderedElement);
                            }
                        }
                    }
                    break;

                case "static":
                default:
                    foreach (var element in section.Elements)
                    {
                        var val = !string.IsNullOrEmpty(element.Source)
                            ? GetValueFromPath(data, element.Source!)?.ToString()
                            : element.StaticValue;

                        var textValue = $"{(element.Label ?? string.Empty)}{val}";

                        // CONDICIONAL PARA NO MOSTRAR EL CAMPO COPIA CUANDO ES "ORIGINAL"
                        if (element.Label == "COPY:" && val == "ORIGINAL")
                        {
                            textValue = "";
                        }

                        // Normalizar el tipo usando el diccionario (o dejar el original si no se conoce)
                        string rawType = element.Type ?? "Text";
                        string normalizedType = _knownTypes.TryGetValue(rawType, out var known) ? known : rawType;

                        var renderedElement = new RenderedElement
                        {
                            Type = normalizedType,
                            TextValue = textValue,
                            Align = element.Align ?? section.Align,
                            Format = element.Format ?? section.Format
                        };

                        if (normalizedType == "Barcode")
                        {
                            renderedElement.BarcodeValue = val;
                            // Para secciones estáticas, 'data' es el contexto.
                            renderedElement.ProductName = GetValueFromPath(data, "name")?.ToString(); // Context dependent
                            // ... logica similar si fuera necesario, pero usualmente barcodes en static son simples
                        }
                        else if (normalizedType == "QR")
                        {
                            renderedElement.QrValue = val;
                            if (element.Properties != null && element.Properties.ContainsKey("Size"))
                            {
                                int.TryParse(element.Properties["Size"], out int size);
                                renderedElement.Size = size;
                            }
                        }
                        else if (normalizedType == "Image")
                        {
                            renderedElement.Base64Image = val; // El Base64 viene del Source o Static
                        }

                        renderedSection.Elements.Add(renderedElement);
                    }
                    break;
            }

            return renderedSection;
        }

        private object? GetValueFromPath(object data, string path)
        {
            if (string.IsNullOrEmpty(path) || path == "." || data == null) return data;

            var parts = path.Split('.');
            object current = data;

            foreach (var part in parts)
            {
                if (current == null) return null;

                var type = current.GetType();
                var prop = type.GetProperty(part, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

                if (prop == null)
                {
                    var field = type.GetField(part, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                    if (field == null) return null;
                    current = field.GetValue(current);
                }
                else
                {
                    current = prop.GetValue(current);
                }
            }
            return current;
        }

        private TicketContent CreateErrorTicket(string jobId, string message)
        {
            var content = new TicketContent();
            var section = new RenderedSection { Name = "Error", Type = "Static" };
            section.Elements.Add(new RenderedElement { Type = "Text", TextValue = "ERROR DE CONFIGURACIÓN", Format = "Bold", Align = "Center" });
            section.Elements.Add(new RenderedElement { Type = "Text", TextValue = message, Align = "Center" });
            content.Sections.Add(section);
            return content;
        }

        private void AddPrintJobMediaToTicketContent(PrintJobRequest request, TicketContent ticketContent)
        {
            if (request.Images != null)
            {
                foreach (var img in request.Images)
                    ticketContent.ExtraMedia.Add(new RenderedElement { Type = "Image", Base64Image = img.Base64, Align = img.Align, WidthMm = img.WidthMm });
            }

            if (request.Barcodes != null)
            {
                foreach (var bc in request.Barcodes)
                {
                    ticketContent.ExtraMedia.Add(new RenderedElement
                    {
                        Type = "Barcode",
                        BarcodeValue = bc.Value,
                        BarcodeType = bc.Type,
                        Height = bc.Height,
                        BarWidth = bc.Width,
                        Hri = bc.Hri,
                        ProductName = bc.Name,
                        ItemId = bc.ItemId,
                        ProductPrice = bc.Price
                    });
                }
            }

            if (request.QRs != null)
            {
                foreach (var qr in request.QRs)
                    ticketContent.ExtraMedia.Add(new RenderedElement { Type = "QR", QrValue = qr.Value, Align = qr.Align, Size = qr.Size });
            }
        }
    }
}