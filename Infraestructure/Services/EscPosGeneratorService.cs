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
        private const byte ESC = 0x1B;
        private const byte GS = 0x1D;

        public EscPosGeneratorService(ILoggingService logger)
        {
            _logger = logger;
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
                    // Lógica de Renderizado de Tablas
                    commands.AddRange(SetAlignment("Left"));

                    foreach (var row in section.TableRows)
                    {
                        if (row.Count == 0) continue;

                        string font = row[0].Format?.Contains("FontB", StringComparison.OrdinalIgnoreCase) == true ? "FontB" : "FontA";
                        int baseCharsPerLine = GetCharsPerLine(printerSettings.PaperWidthMm, font);

                        int[] columnWidths = new int[row.Count];
                        int totalPctUsed = 0;
                        int colsWithoutPct = 0;

                        for (int i = 0; i < row.Count; i++)
                        {
                            if (row[i].WidthPercentage.HasValue) totalPctUsed += row[i].WidthPercentage!.Value;
                            else colsWithoutPct++;
                        }

                        int remainingPct = Math.Max(0, 100 - totalPctUsed);
                        int pctPerMissingCol = colsWithoutPct > 0 ? remainingPct / colsWithoutPct : 0;

                        for (int i = 0; i < row.Count; i++)
                        {
                            columnWidths[i] = (int)(baseCharsPerLine * ((row[i].WidthPercentage ?? pctPerMissingCol) / 100.0));
                        }

                        var columnLines = new List<List<string>>();
                        int maxLinesCount = 0;
                        int[] multipliers = new int[row.Count];

                        for (int i = 0; i < row.Count; i++)
                        {
                            multipliers[i] = GetSizeMultiplier(row[i].Format);
                            int wrapWidth = Math.Max(1, columnWidths[i] / multipliers[i]);
                            var text = row[i].TextValue ?? string.Empty;
                            var wrappedBody = WrapText(text, wrapWidth);
                            columnLines.Add(wrappedBody);
                            if (wrappedBody.Count > maxLinesCount) maxLinesCount = wrappedBody.Count;
                        }

                        for (int lineIdx = 0; lineIdx < maxLinesCount; lineIdx++)
                        {
                            for (int colIdx = 0; colIdx < row.Count; colIdx++)
                            {
                                var element = row[colIdx];
                                var lineText = lineIdx < columnLines[colIdx].Count ? columnLines[colIdx][lineIdx] : "";
                                commands.AddRange(SetTextFormat(element.Format));
                                int effectiveWidth = Math.Max(1, columnWidths[colIdx] / multipliers[colIdx]);
                                if (element.Align?.Equals("Right", StringComparison.OrdinalIgnoreCase) == true)
                                    commands.AddRange(Encoding.UTF8.GetBytes(lineText.PadLeft(effectiveWidth)));
                                else
                                    commands.AddRange(Encoding.UTF8.GetBytes(lineText.PadRight(effectiveWidth)));
                                commands.AddRange(ResetTextFormat());
                            }
                            commands.Add(0x0A);
                        }
                    }
                }
                else // Static (Default)
                {
                    foreach (var element in section.Elements)
                    {
                        commands.AddRange(ProcessRenderedElement(element, printerSettings));
                    }
                }
            }

            // --- MEDIA ADICIONAL ---
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

        private byte[] InitializePrinter() => new byte[] { ESC, 0x40 };

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

        private byte[] ResetTextFormat() => new byte[] { ESC, 0x45, 0x00, ESC, 0x2D, 0x00, GS, 0x21, 0x00, ESC, 0x4D, 0x00 };

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
                        elementCommands.AddRange(Encoding.UTF8.GetBytes(element.TextValue));
                        elementCommands.Add(0x0A);
                    }
                    break;
                case "Line":
                    int chars = GetCharsPerLine(printerSettings.PaperWidthMm, element.Format?.Contains("FontB") == true ? "FontB" : "FontA");
                    elementCommands.AddRange(Encoding.UTF8.GetBytes(new string('-', chars)));
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

        private int GetCharsPerLine(int paperWidthMm, string font = "FontA")
        {
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
            cmds.AddRange(new byte[] { GS, 0x68, (byte)(element.Height ?? 50) });
            cmds.AddRange(new byte[] { GS, 0x48, (byte)(element.Hri == true ? 0x02 : 0x00) });
            byte[] data = Encoding.UTF8.GetBytes(element.BarcodeValue ?? "");
            cmds.AddRange(new byte[] { GS, 0x6B, 0x49, (byte)(data.Length + 2), 0x7B, 0x42 });
            cmds.AddRange(data);
            cmds.Add(0x0A);
            return cmds.ToArray();
        }

        private byte[] HandleQR(RenderedElement element, PrinterSettings printerSettings)
        {
            var cmds = new List<byte>();
            byte[] data = Encoding.UTF8.GetBytes(element.QrValue ?? "");
            cmds.AddRange(new byte[] { GS, 0x28, 0x6B, 0x04, 0x00, 0x31, 0x41, 0x32, 0x00 });
            byte size = (byte)Math.Clamp(element.Size ?? 3, 1, 16);
            cmds.AddRange(new byte[] { GS, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x43, size });
            cmds.AddRange(new byte[] { GS, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x45, 0x32 });
            int len = data.Length;
            cmds.AddRange(new byte[] { GS, 0x28, 0x6B, (byte)(len % 256), (byte)(len / 256), 0x31, 0x50, 0x30 });
            cmds.AddRange(data);
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
