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
    /// para documentos pre-impresos.
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

            // Crear el lienzo (Canvas) de caracteres inicializado con espacios
            char[][] canvas = new char[template.TotalRows][];
            for (int i = 0; i < template.TotalRows; i++)
            {
                canvas[i] = new string(' ', template.TotalColumns).ToCharArray();
            }

            foreach (var element in template.Elements)
            {
                try
                {
                    if (!string.IsNullOrEmpty(element.DataSource))
                    {
                        // Manejo de listas (Secciones repetidas)
                        var listData = GetValueFromPath(documentData, element.DataSource) as IEnumerable;
                        if (listData != null)
                        {
                            int currentRow = element.Row;
                            foreach (var item in listData)
                            {
                                if (currentRow > template.TotalRows) break;

                                var val = GetValueFromPath(item, element.Source ?? string.Empty)?.ToString() ?? string.Empty;
                                WriteAt(canvas, currentRow, element.Column, val, element, template);

                                currentRow += (element.RowIncrement ?? 1);
                            }
                        }
                    }
                    else
                    {
                        // Elemento simple
                        var val = !string.IsNullOrEmpty(element.Source)
                            ? GetValueFromPath(documentData, element.Source)?.ToString()
                            : element.StaticValue ?? string.Empty;

                        string finalValue = $"{element.Label}{val}";
                        WriteAt(canvas, element.Row, element.Column, finalValue, element, template);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error al procesar elemento matricial: {ex.Message}");
                }
            }

            // Convertir canvas a string con saltos de linea
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < template.TotalRows; i++)
            {
                sb.AppendLine(new string(canvas[i]));
            }

            return Task.FromResult(sb.ToString());
        }

        private void WriteAt(char[][] canvas, int row, int col, string value, DotMatrixElement element, DotMatrixTemplate template)
        {
            // Ajustar a 0-indexado
            int r = row - 1;
            int c = col - 1;

            if (r < 0 || r >= template.TotalRows || c < 0 || c >= template.TotalColumns) return;

            string text = value;

            // 1. Aplicar Padding si está configurado
            if (element.PaddingChar.HasValue && element.MaxLength > 0)
            {
                if (element.PaddingType?.Equals("Left", StringComparison.OrdinalIgnoreCase) == true)
                {
                    text = text.PadLeft(element.MaxLength, element.PaddingChar.Value);
                }
                else
                {
                    text = text.PadRight(element.MaxLength, element.PaddingChar.Value);
                }
            }

            // 2. Manejo de MaxLength (sin wrapping) o preparación para Wrapping
            int primaryLimit = element.MaxLength > 0 ? element.MaxLength : (template.TotalColumns - c);

            string primaryPart = text;
            string overflowPart = string.Empty;

            if (text.Length > primaryLimit)
            {
                primaryPart = text.Substring(0, primaryLimit);
                overflowPart = text.Substring(primaryLimit);
            }

            // 3. Escribir parte principal
            int lengthToCopy = Math.Min(primaryPart.Length, template.TotalColumns - c);
            for (int i = 0; i < lengthToCopy; i++)
            {
                canvas[r][c + i] = primaryPart[i];
            }

            // 4. Manejo de Wrapping (Salto de línea a coordenadas específicas)
            if (element.WrapToRow.HasValue && element.WrapToColumn.HasValue)
            {
                int wrapR = element.WrapToRow.Value - 1;
                int wrapC = element.WrapToColumn.Value - 1;

                if (wrapR >= 0 && wrapR < template.TotalRows && wrapC >= 0 && wrapC < template.TotalColumns)
                {
                    int wrapLimit = element.WrapMaxLength ?? (template.TotalColumns - wrapC);
                    string secondPart = overflowPart;

                    if (secondPart.Length > wrapLimit)
                    {
                        secondPart = secondPart.Substring(0, wrapLimit);
                    }

                    // Aplicar padding a la segunda parte si es necesario (garantiza asteriscos aunque no haya overflow)
                    if (element.PaddingChar.HasValue && wrapLimit > 0)
                    {
                        secondPart = secondPart.PadRight(wrapLimit, element.PaddingChar.Value);
                    }

                    if (!string.IsNullOrEmpty(secondPart))
                    {
                        int lengthToCopyWrap = Math.Min(secondPart.Length, template.TotalColumns - wrapC);
                        for (int i = 0; i < lengthToCopyWrap; i++)
                        {
                            canvas[wrapR][wrapC + i] = secondPart[i];
                        }
                    }
                }
            }
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
