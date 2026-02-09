using AppsielPrintManager.Core.Interfaces;
using AppsielPrintManager.Core.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppsielPrintManager.Infraestructure.Services
{
    /// <summary>
    /// Implementación de IEscPosGenerator.
    /// Encargado de convertir un objeto TicketContent estructurado en una secuencia
    /// de bytes de comandos ESC/POS listos para ser enviados a la impresora.
    /// </summary>
    public class EscPosGeneratorService : IEscPosGenerator
    {
        private readonly ILoggingService _logger;
        private readonly Encoding _encoding;
        private const byte ESC = 0x1B;
        private const byte GS = 0x1D;

        public EscPosGeneratorService(ILoggingService logger)
        {
            _logger = logger;
            // Registrar proveedor para soportar Code Pages de Windows (como 1252)
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            try
            {
                // Usaremos Windows-1252 (WPC1252) que es un estándar muy común en impresoras POS
                _encoding = Encoding.GetEncoding(1252);
            }
            catch
            {
                _logger.LogWarning("No se pudo cargar el Code Page 1252. Usando fallback.");
                try { _encoding = Encoding.GetEncoding("ISO-8859-1"); }
                catch { _encoding = Encoding.UTF8; } // Último recurso, pero el mapeo manual en GetPosBytes salvará el día
            }
        }

        public Task<byte[]> GenerateEscPosCommandsAsync(TicketContent ticketContent, PrinterSettings printerSettings)
        {
            _logger.LogInfo($"Generando comandos ESC/POS para impresora {printerSettings.PrinterId} (ancho: {printerSettings.PaperWidthMm}mm).");

            var commands = new List<byte>();

            // Inicializar impresora
            commands.AddRange(InitializePrinter());

            // --- PROCESAMIENTO SECUENCIAL DE SECCIONES ---
            foreach (var section in ticketContent.Sections)
            {
                if (section.Type?.Equals("Table", StringComparison.OrdinalIgnoreCase) == true)
                {
                    commands.AddRange(SetAlignment("Left"));

                    foreach (var row in section.TableRows)
                    {
                        if (row.Count == 0) continue;

                        int totalPctUsed = 0;
                        int colsWithoutPct = 0;
                        for (int i = 0; i < row.Count; i++)
                        {
                            if (row[i].WidthPercentage.HasValue) totalPctUsed += row[i].WidthPercentage!.Value;
                            else colsWithoutPct++;
                        }
                        int remainingPct = Math.Max(0, 100 - totalPctUsed);
                        int pctPerMissingCol = colsWithoutPct > 0 ? remainingPct / colsWithoutPct : 0;

                        var columnLines = new List<List<string>>();
                        int maxLinesCount = 0;
                        int[] wrapWidths = new int[row.Count];
                        string[] cellFonts = new string[row.Count];

                        for (int i = 0; i < row.Count; i++)
                        {
                            var element = row[i];
                            cellFonts[i] = element.Format?.Contains("FontB", StringComparison.OrdinalIgnoreCase) == true ? "FontB" : "FontA";
                            int charsPerLine = GetCharsPerLine(printerSettings.PaperWidthMm, cellFonts[i]);
                            int multiplier = GetSizeMultiplier(element.Format);

                            // Ancho lógico disponible en "slots" de la fuente actual para este % físico
                            double pct = (element.WidthPercentage ?? pctPerMissingCol) / 100.0;
                            wrapWidths[i] = (int)((charsPerLine * pct) / multiplier);
                            if (wrapWidths[i] < 1) wrapWidths[i] = 1;

                            var text = element.TextValue ?? string.Empty;
                            var wrappedBody = WrapText(text, wrapWidths[i]);
                            columnLines.Add(wrappedBody);
                            if (wrappedBody.Count > maxLinesCount) maxLinesCount = wrappedBody.Count;
                        }

                        // Imprimir cada línea física de la fila lógica
                        for (int lineIdx = 0; lineIdx < maxLinesCount; lineIdx++)
                        {
                            for (int colIdx = 0; colIdx < row.Count; colIdx++)
                            {
                                var element = row[colIdx];
                                var lineText = lineIdx < columnLines[colIdx].Count ? columnLines[colIdx][lineIdx] : "";

                                // 1. Aplicar formato específico de la celda (incluyendo fuente)
                                commands.AddRange(SetTextFormat(element.Format));
                                if (cellFonts[colIdx] == "FontB") commands.AddRange(new byte[] { ESC, 0x4D, 0x01 });
                                else commands.AddRange(new byte[] { ESC, 0x4D, 0x00 });

                                // 2. Alinear y Padechar texto
                                string paddedText;
                                if (element.Align?.Equals("Right", StringComparison.OrdinalIgnoreCase) == true)
                                    paddedText = lineText.PadLeft(wrapWidths[colIdx]);
                                else if (element.Align?.Equals("Center", StringComparison.OrdinalIgnoreCase) == true)
                                {
                                    int left = (wrapWidths[colIdx] - lineText.Length) / 2;
                                    paddedText = lineText.PadLeft(lineText.Length + left).PadRight(wrapWidths[colIdx]);
                                }
                                else
                                    paddedText = lineText.PadRight(wrapWidths[colIdx]);

                                commands.AddRange(GetPosBytes(paddedText));

                                // 3. Reset para la siguiente celda
                                commands.AddRange(ResetTextFormat());
                            }
                            commands.Add(0x0A); // Salto de línea física
                        }
                    }
                    commands.AddRange(ResetTextFormat());
                }
                else // Static (Default)
                {
                    foreach (var element in section.Elements)
                    {
                        commands.AddRange(ProcessRenderedElement(element, printerSettings));
                    }
                }
            }

            // --- MEDIA ADICIONAL (Logo, etc) ---
            foreach (var element in ticketContent.ExtraMedia)
            {
                commands.AddRange(ProcessRenderedElement(element, printerSettings));
            }

            commands.AddRange(FeedLines(5));
            if (printerSettings.OpenCashDrawerWithoutPrint) commands.AddRange(OpenCashDrawer());
            if (printerSettings.BeepOnPrint) commands.AddRange(GenerateBeep());
            commands.AddRange(CutPaper());
            if (printerSettings.OpenCashDrawerAfterPrint) commands.AddRange(OpenCashDrawer());

            return Task.FromResult(commands.ToArray());
        }

        private byte[] InitializePrinter()
        {
            var cmd = new List<byte>();
            cmd.AddRange(new byte[] { ESC, 0x40 }); // ESC @ - Inicializar

            // 1. DESACTIVACIÓN DE MODOS MULTIBYTE (Crucial para evitar símbolos chinos/gráficos)
            cmd.AddRange(new byte[] { 0x1C, 0x2E }); // FS . - Cancelar modo Kanji
            cmd.AddRange(new byte[] { ESC, 0x39, 0x00 }); // ESC 9 0 - Desactivar modo de caracteres chinos en algunos modelos

            // 2. SET DE CARACTERES INTERNACIONALES (España)
            // ESC R n - Selecciona el set de caracteres internacionales (n=7 es España)
            cmd.AddRange(new byte[] { ESC, 0x52, 0x07 });

            cmd.AddRange(new byte[] { ESC, 0x4D, 0x00 }); // Font A por defecto

            // 3. SELECCIÓN DE TABLA DE CARACTERES (Code Page)
            // n = 16 (0x10) suele ser WPC1252 (Windows-1252) que mapea con Encoding 1252
            cmd.AddRange(new byte[] { ESC, 0x74, 0x10 });

            return cmd.ToArray();
        }

        private byte[] SetAlignment(string? align)
        {
            byte n = 0x00; // Left
            if (align?.Equals("Center", StringComparison.OrdinalIgnoreCase) == true) n = 0x01;
            else if (align?.Equals("Right", StringComparison.OrdinalIgnoreCase) == true) n = 0x02;
            return new byte[] { ESC, 0x61, n };
        }

        private byte[] SetTextFormat(string? format)
        {
            var cmd = new List<byte>();
            byte fontSize = 0x00;

            if (string.IsNullOrEmpty(format)) return cmd.ToArray();

            // Solo cambiamos la fuente si el formato lo pide explícitamente
            if (format.Contains("FontB", StringComparison.OrdinalIgnoreCase))
                cmd.AddRange(new byte[] { ESC, 0x4D, 0x01 });
            else if (format.Contains("FontA", StringComparison.OrdinalIgnoreCase))
                cmd.AddRange(new byte[] { ESC, 0x4D, 0x00 });

            if (format.Contains("Bold", StringComparison.OrdinalIgnoreCase))
                cmd.AddRange(new byte[] { ESC, 0x45, 0x01 });

            if (format.Contains("Underline", StringComparison.OrdinalIgnoreCase))
                cmd.AddRange(new byte[] { ESC, 0x2D, 0x01 });

            int multiplier = GetSizeMultiplier(format);
            if (multiplier > 1)
            {
                byte s = (byte)(multiplier - 1);
                fontSize = (byte)((s << 4) | s);
            }
            else
            {
                if (format.Contains("Large", StringComparison.OrdinalIgnoreCase)) fontSize |= 0x01;
                if (format.Contains("DoubleWidth", StringComparison.OrdinalIgnoreCase)) fontSize |= 0x10;
            }

            if (fontSize != 0x00) cmd.AddRange(new byte[] { GS, 0x21, fontSize });

            return cmd.ToArray();
        }

        private byte[] ResetTextFormat()
        {
            // NOTA: NO reseteamos la fuente aquí (ESC M) para evitar solapamientos en tablas
            return new byte[] { ESC, 0x45, 0x00, ESC, 0x2D, 0x00, GS, 0x21, 0x00 };
        }

        private byte[] FeedLines(int lines) => new byte[] { ESC, 0x64, (byte)Math.Clamp(lines, 1, 255) };

        private byte[] CutPaper() => new byte[] { GS, 0x56, 0x00 };

        private byte[] OpenCashDrawer() => new byte[] { ESC, 0x70, 0x00, 0x32, 0x32 };

        private byte[] ProcessRenderedElement(RenderedElement element, PrinterSettings printerSettings)
        {
            var elementCommands = new List<byte>();
            elementCommands.AddRange(SetAlignment(element.Align));
            elementCommands.AddRange(SetTextFormat(element.Format));

            switch (element.Type)
            {
                case "Text":
                    if (!string.IsNullOrEmpty(element.TextValue))
                    {
                        elementCommands.AddRange(GetPosBytes(element.TextValue));
                        elementCommands.Add(0x0A);
                    }
                    break;
                case "Line":
                    int chars = GetCharsPerLine(printerSettings.PaperWidthMm, element.Format?.Contains("FontB") == true ? "FontB" : "FontA");
                    elementCommands.AddRange(GetPosBytes(new string('-', chars)));
                    elementCommands.Add(0x0A);
                    break;
                case "Barcode":
                    elementCommands.AddRange(HandleBarcode(element, printerSettings));
                    break;
                case "QR":
                    elementCommands.AddRange(HandleQR(element, printerSettings));
                    break;
            }

            elementCommands.AddRange(ResetTextFormat());
            return elementCommands.ToArray();
        }

        /// <summary>
        /// Método de seguridad que garantiza el envío de bytes correctos para el español.
        /// Si la codificación actual es UTF-8 (error del provider), mapea manualmente a Windows-1252.
        /// </summary>
        private byte[] GetPosBytes(string text)
        {
            if (string.IsNullOrEmpty(text)) return Array.Empty<byte>();

            // Si detectamos que el sistema nos está dando UTF-8 o algo que no es 1252/850, 
            // forzamos el mapeo de los bytes críticos del español.
            if (_encoding.WebName.Contains("utf", StringComparison.OrdinalIgnoreCase) ||
                _encoding.WebName.Equals("us-ascii", StringComparison.OrdinalIgnoreCase))
            {
                var bytes = new List<byte>();
                foreach (char c in text)
                {
                    if (c < 128) bytes.Add((byte)c);
                    else
                    {
                        // Mapeo manual a Windows-1252
                        byte b = c switch
                        {
                            'ñ' => 0xF1,
                            'Ñ' => 0xD1,
                            'á' => 0xE1,
                            'é' => 0xE9,
                            'í' => 0xED,
                            'ó' => 0xF3,
                            'ú' => 0xFA,
                            'Á' => 0xC1,
                            'É' => 0xC9,
                            'Í' => 0xCD,
                            'Ó' => 0xD3,
                            'Ú' => 0xDA,
                            '¿' => 0xBF,
                            '¡' => 0xA1,
                            _ => 0x3F // ?
                        };
                        bytes.Add(b);
                    }
                }
                return bytes.ToArray();
            }

            return _encoding.GetBytes(text);
        }

        private int GetCharsPerLine(int paperWidthMm, string font = "FontA")
        {
            // Basado en el reporte físico enviado: 48-A / 64-B para 80mm (72mm print width)
            if (font == "FontB") return paperWidthMm >= 80 ? 64 : 42;
            return paperWidthMm >= 80 ? 48 : 32;
        }

        private int GetSizeMultiplier(string? format)
        {
            if (string.IsNullOrEmpty(format)) return 1;
            var parts = format.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var sizePart = parts.FirstOrDefault(p => p.StartsWith("Size", StringComparison.OrdinalIgnoreCase));
            if (sizePart != null && int.TryParse(sizePart.AsSpan(4), out int s)) return Math.Clamp(s, 1, 8);
            if (format.Contains("DoubleWidth", StringComparison.OrdinalIgnoreCase)) return 2;
            return 1;
        }

        private byte[] GenerateBeep() => new byte[] { GS, 0x28, 0x45, 0x04, 0x00, 0x01, 0x01, 0x01, 0x01 };

        private byte[] HandleBarcode(RenderedElement element, PrinterSettings printerSettings)
        {
            var cmds = new List<byte>();

            // 1. Imprimir Nombre del Producto Arriba (Si existe)
            if (!string.IsNullOrEmpty(element.ProductName))
            {
                cmds.AddRange(SetAlignment("Center"));
                cmds.AddRange(SetTextFormat("FontA")); // Fuente legible estándar
                cmds.AddRange(GetPosBytes(element.ProductName));
                cmds.Add(0x0A); // Salto de línea
            }

            // 2. Imprimir Código de Barras
            // Nota: El alineamiento ya debería estar seteado, pero aseguramos Center si no se especificó
            cmds.AddRange(SetAlignment(element.Align ?? "Center"));
            cmds.AddRange(new byte[] { GS, 0x68, (byte)(element.Height ?? 50) }); // Altura

            // HRI (Human Readable Interpretation) - Números debajo de las barras
            // 0x00 = Ninguno, 0x02 = Debajo
            cmds.AddRange(new byte[] { GS, 0x48, (byte)(element.Hri == true ? 0x02 : 0x00) });

            byte[] data = GetPosBytes(element.BarcodeValue ?? "");

            // CODE128 (Tipo B genérico, simplificado)
            // GS k m n d1...dn
            // m=73 (CODE128)
            // Calculamos longitud + 2 bytes de cabecera CODE128 (ej {B + data)
            // Para simplificar, asumimos que el cliente envía datos limpios o usamos CODE128 auto (más complejo).
            // Mantenemos la lógica original simple: GS k I (Code128) len {B data 
            cmds.AddRange(new byte[] { GS, 0x6B, 0x49, (byte)(data.Length + 2), 0x7B, 0x42 });
            cmds.AddRange(data);
            cmds.Add(0x0A); // Un salto de línea post-barcode suele ser necesario para evitar solapamiento

            // 3. Imprimir ItemId y Precio Abajo (Si existen)
            if (!string.IsNullOrEmpty(element.ItemId) || !string.IsNullOrEmpty(element.ProductPrice))
            {
                cmds.AddRange(SetAlignment("Center"));
                cmds.AddRange(SetTextFormat("FontB")); // Fuente un poco más pequeña para info secundaria

                var parts = new List<string>();
                if (!string.IsNullOrEmpty(element.ItemId)) parts.Add(element.ItemId);
                if (!string.IsNullOrEmpty(element.ProductPrice)) parts.Add(element.ProductPrice);

                string footerText = string.Join(" | ", parts);
                cmds.AddRange(GetPosBytes(footerText));
                cmds.Add(0x0A);
            }

            return cmds.ToArray();
        }

        private byte[] HandleQR(RenderedElement element, PrinterSettings printerSettings)
        {
            var cmds = new List<byte>();
            byte[] data = GetPosBytes(element.QrValue ?? "");

            // 1. Configurar Modelo QR: Usar Modelo 2 (Mejor compatibilidad y eficiencia)
            // GS ( k pL pH cn fn n1 n2 --> cn=49 (31 hex), fn=65 (41 hex) para seleccionar modelo
            // n1=50 (32 hex) = Modelo 2
            cmds.AddRange(new byte[] { GS, 0x28, 0x6B, 0x04, 0x00, 0x31, 0x41, 0x32, 0x00 });

            // 2. Configurar Tamaño del Módulo (Size)
            // Range recomendado: 1 a 16. Para URLs largas, mejor usar 3 o 4.
            // Si el usuario puso algo > 6 en la plantilla, lo forzamos a 4 para prevenir overflow.
            int requestedSize = element.Size ?? 3;
            if (requestedSize > 8) requestedSize = 4; // Protección anti-overflow
            byte size = (byte)Math.Clamp(requestedSize, 1, 16);

            cmds.AddRange(new byte[] { GS, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x43, size });

            // 3. Nivel de Corrección de Errores
            // Cambiamos a Nivel 'L' (7%) en lugar de 'M' o 'H', para que el QR sea menos denso y quepa mejor.
            // 48 (30 hex) = L, 49 (31 hex) = M, 50 (32 hex) = Q, 51 (33 hex) = H
            cmds.AddRange(new byte[] { GS, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x45, 0x30 }); // Nivel L (0x30)

            // 4. Almacenar datos del QR
            // GS ( k pL pH cn fn m d1...dk
            int len = data.Length + 3; // +3 por los parametros m, d1...dk
            int pL = len % 256;
            int pH = len / 256;

            cmds.AddRange(new byte[] { GS, 0x28, 0x6B, (byte)pL, (byte)pH, 0x31, 0x50, 0x30 });
            cmds.AddRange(data);

            // 5. Imprimir el símbolo QR
            cmds.AddRange(new byte[] { GS, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x51, 0x30 });

            cmds.Add(0x0A);
            return cmds.ToArray();
        }

        private List<string> WrapText(string text, int width)
        {
            if (string.IsNullOrEmpty(text)) return new List<string> { "" };
            if (width <= 0) return new List<string> { text };
            var lines = new List<string>();
            for (int i = 0; i < text.Length; i += width)
                lines.Add(text.Substring(i, Math.Min(width, text.Length - i)));
            return lines;
        }
    }
}
