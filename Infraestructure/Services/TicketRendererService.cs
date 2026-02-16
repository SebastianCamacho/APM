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

            var orderedElements = section.Elements
                .OrderBy(e => e.Order ?? 999)
                .ThenBy(e => section.Elements.IndexOf(e))
                .ToList();

            switch (section.Type?.ToLower())
            {
                case "table":
                    var listData = GetValueFromPath(data, section.DataSource ?? string.Empty) as IEnumerable;
                    if (listData != null)
                    {
                        // Encabezados de tabla: Heredan de la sección si no hay HeaderFormat
                        var headers = orderedElements.Select(e => new RenderedElement
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
                            var row = orderedElements.Select(e => new RenderedElement
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
                            foreach (var element in orderedElements)
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
                                    Format = element.Format ?? section.Format,
                                    Columns = element.Columns
                                };

                                if (normalizedType == "Barcode")
                                {
                                    PopulateBarcodeProperties(element, renderedElement, item);
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
                    foreach (var element in orderedElements)
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
                            Format = element.Format ?? section.Format,
                            Columns = element.Columns
                        };

                        if (normalizedType == "Barcode")
                        {
                            PopulateBarcodeProperties(element, renderedElement, data);
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

        private void PopulateBarcodeProperties(TemplateElement element, RenderedElement rendered, object? item)
        {
            rendered.BarcodeValue = !string.IsNullOrEmpty(element.Source) && element.Source != "."
                ? GetValueFromPath(item, element.Source)?.ToString()
                : (element.Source == "." ? item?.ToString() : element.StaticValue);

            // 1. Mapeo de campos de texto (Nombre, ID, Precio)
            string nameField = (element.Properties != null && element.Properties.ContainsKey("NameSource")) ? element.Properties["NameSource"] : "Name";
            string idField = (element.Properties != null && element.Properties.ContainsKey("ItemIdSource")) ? element.Properties["ItemIdSource"] : "ItemId";
            string priceField = (element.Properties != null && element.Properties.ContainsKey("PriceSource")) ? element.Properties["PriceSource"] : "Price";

            rendered.ProductName = GetValueFromPath(item, nameField)?.ToString();
            rendered.ItemId = GetValueFromPath(item, idField)?.ToString();
            rendered.ProductPrice = GetValueFromPath(item, priceField)?.ToString();

            // 2. PRIORIDAD DE PROPIEDADES (Template Root > Template Properties > Data)
            var props = element.Properties != null ? new Dictionary<string, string>(element.Properties, StringComparer.OrdinalIgnoreCase) : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // HEIGHT: Priorizar el campo directo (el que usa el slider/campo del editor)
            int? tHeight = element.Height;
            if (!tHeight.HasValue && props.ContainsKey("Height") && int.TryParse(props["Height"], out int h)) tHeight = h;

            var dataHeightRaw = GetValueFromPath(item, "Height");
            int? dHeight = dataHeightRaw as int? ?? (int.TryParse(dataHeightRaw?.ToString(), out int h2) ? h2 : null);

            rendered.Height = tHeight ?? dHeight;
            _logger.LogInfo($"[TicketRenderer] Barcode Height Result: {rendered.Height} (Root: {element.Height}, AdvProps: {(props.ContainsKey("Height") ? props["Height"] : "N/A")}, Data: {dHeight})");

            // HRI
            bool? tHri = null;
            if (props.ContainsKey("Hri") && bool.TryParse(props["Hri"], out bool b)) tHri = b;

            var dataHriRaw = GetValueFromPath(item, "Hri");
            bool? dHri = dataHriRaw as bool? ?? (bool.TryParse(dataHriRaw?.ToString(), out bool b2) ? b2 : null);

            rendered.Hri = tHri ?? dHri;

            // WIDTH / BARWIDTH
            int? tWidth = element.BarWidth;
            if (!tWidth.HasValue && props.ContainsKey("Width") && int.TryParse(props["Width"], out int w)) tWidth = w;
            if (!tWidth.HasValue && props.ContainsKey("BarWidth") && int.TryParse(props["BarWidth"], out int w3)) tWidth = w3;

            var dataWidthRaw = GetValueFromPath(item, "Width");
            int? dWidth = dataWidthRaw as int? ?? (int.TryParse(dataWidthRaw?.ToString(), out int w2) ? w2 : null);

            rendered.BarWidth = tWidth ?? dWidth;
            _logger.LogInfo($"[TicketRenderer] Barcode Width Result: {rendered.BarWidth} (Root: {element.BarWidth}, AdvProps: {(props.ContainsKey("Width") ? props["Width"] : "N/A")}, Data: {dWidth})");
        }

        private object? GetValueFromPath(object? data, string path)
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