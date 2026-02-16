using AppsielPrintManager.Core.Interfaces;
using AppsielPrintManager.Core.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AppsielPrintManager.Infraestructure.Services
{
    /// <summary>
    /// Renderizador que genera una grilla de texto (matriz de caracteres)
    /// para documentos pre-impresos con soporte para alturas de fila dinámicas.
    /// </summary>
    public class DotMatrixRendererService
    {
        private readonly ILoggingService _logger;
        private readonly ITemplateRepository _templateRepository;

        public DotMatrixRendererService(ILoggingService logger, ITemplateRepository templateRepository)
        {
            _logger = logger;
            _templateRepository = templateRepository;
        }

        public Task<string> RenderToStringAsync(PrintJobRequest request, object documentData, DotMatrixTemplate template)
        {
            _logger.LogInfo($"Renderizando grilla matricial para {request.DocumentType} ({template.TotalRows}x{template.TotalColumns})");

            char[][] canvas = new char[template.TotalRows][];
            for (int i = 0; i < template.TotalRows; i++)
            {
                canvas[i] = new string(' ', template.TotalColumns).ToCharArray();
            }

            var processedDataSources = new HashSet<string>();

            foreach (var element in template.Elements)
            {
                try
                {
                    if (!string.IsNullOrEmpty(element.DataSource))
                    {
                        if (processedDataSources.Contains(element.DataSource)) continue;

                        // Procesar el grupo completo del DataSource para manejar Row Height dinámico
                        var group = template.Elements.Where(e => e.DataSource == element.DataSource).ToList();
                        var listData = GetValueFromPath(documentData, element.DataSource) as IEnumerable;

                        if (listData != null)
                        {
                            int baseRow = group.Min(e => e.Row);
                            int currentBaseRow = baseRow;

                            foreach (var item in listData)
                            {
                                if (currentBaseRow > template.TotalRows) break;

                                int maxLinesInThisLogicalRow = 1;

                                foreach (var groupElement in group)
                                {
                                    var val = GetValueFromPath(item, groupElement.Source ?? string.Empty)?.ToString() ?? string.Empty;
                                    int relativeRowOffset = groupElement.Row - baseRow;
                                    int startRow = currentBaseRow + relativeRowOffset;

                                    int linesUsed = RenderElement(canvas, startRow, groupElement.Column, val, groupElement, template);

                                    int heightContribution = relativeRowOffset + linesUsed;
                                    if (heightContribution > maxLinesInThisLogicalRow)
                                        maxLinesInThisLogicalRow = heightContribution;
                                }

                                // El siguiente item de la lista comienza después de la línea más larga de esta fila lógica
                                currentBaseRow += maxLinesInThisLogicalRow;
                            }
                        }

                        processedDataSources.Add(element.DataSource);
                    }
                    else
                    {
                        // Elemento simple
                        var val = !string.IsNullOrEmpty(element.Source)
                            ? GetValueFromPath(documentData, element.Source)?.ToString()
                            : element.StaticValue ?? string.Empty;

                        string finalValue = $"{element.Label}{val}";
                        RenderElement(canvas, element.Row, element.Column, finalValue, element, template);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error al procesar elemento matricial: {ex.Message}");
                }
            }

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < template.TotalRows; i++)
            {
                sb.AppendLine(new string(canvas[i]));
            }

            return Task.FromResult(sb.ToString());
        }

        private int RenderElement(char[][] canvas, int row, int col, string value, DotMatrixElement element, DotMatrixTemplate template)
        {
            int r = row - 1;
            int c = col - 1;

            if (r < 0 || r >= template.TotalRows || c < 0 || c >= template.TotalColumns) return 0;

            string text = value;

            // 1. Aplicar Padding si está configurado
            if (element.PaddingChar.HasValue && element.MaxLength > 0)
            {
                if (element.PaddingType?.Equals("Left", StringComparison.OrdinalIgnoreCase) == true)
                    text = text.PadLeft(element.MaxLength, element.PaddingChar.Value);
                else
                    text = text.PadRight(element.MaxLength, element.PaddingChar.Value);
            }

            // 2. Manejo de Multilínea Automática (AutoWrap)
            if (element.EnableAutoWrap == true && element.MaxLength > 0)
            {
                int linesUsed = 0;
                for (int i = 0; i < text.Length; i += element.MaxLength)
                {
                    int targetRow = r + linesUsed;
                    if (targetRow >= template.TotalRows) break;

                    string chunk = text.Substring(i, Math.Min(element.MaxLength, text.Length - i));

                    if (element.PaddingChar.HasValue)
                        chunk = chunk.PadRight(element.MaxLength, element.PaddingChar.Value);

                    int lengthToCopy = Math.Min(chunk.Length, template.TotalColumns - c);
                    for (int j = 0; j < lengthToCopy; j++)
                    {
                        canvas[targetRow][c + j] = chunk[j];
                    }
                    linesUsed++;
                }
                return linesUsed > 0 ? linesUsed : 1;
            }

            // 3. Manejo de Wrapping Fijo (2 tramos específicos como el monto en letras)
            int primaryLimit = element.MaxLength > 0 ? element.MaxLength : (template.TotalColumns - c);
            string primaryPart = text.Length > primaryLimit ? text.Substring(0, primaryLimit) : text;
            string overflowPart = text.Length > primaryLimit ? text.Substring(primaryLimit) : string.Empty;

            int primaryLen = Math.Min(primaryPart.Length, template.TotalColumns - c);
            for (int i = 0; i < primaryLen; i++)
            {
                if (r < template.TotalRows && c + i < template.TotalColumns)
                    canvas[r][c + i] = primaryPart[i];
            }

            if (element.WrapToRow.HasValue && element.WrapToColumn.HasValue)
            {
                int wrapR = element.WrapToRow.Value - 1;
                int wrapC = element.WrapToColumn.Value - 1;

                if (wrapR >= 0 && wrapR < template.TotalRows && wrapC >= 0 && wrapC < template.TotalColumns)
                {
                    int wrapLimit = element.WrapMaxLength ?? (template.TotalColumns - wrapC);
                    string secondPart = overflowPart;

                    if (secondPart.Length > wrapLimit)
                        secondPart = secondPart.Substring(0, wrapLimit);

                    if (element.PaddingChar.HasValue && wrapLimit > 0)
                        secondPart = secondPart.PadRight(wrapLimit, element.PaddingChar.Value);

                    if (!string.IsNullOrEmpty(secondPart))
                    {
                        int len = Math.Min(secondPart.Length, template.TotalColumns - wrapC);
                        for (int i = 0; i < len; i++)
                            canvas[wrapR][wrapC + i] = secondPart[i];
                    }
                }
            }

            return 1;
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
                    current = field.GetValue(current)!;
                }
                else
                {
                    current = prop.GetValue(current)!;
                }
            }
            return current;
        }
    }
}
