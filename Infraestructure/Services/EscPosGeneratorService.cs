using AppsielPrintManager.Core.Interfaces;
using AppsielPrintManager.Core.Models;
using System; // Required for StringComparison
using System.Collections.Generic;
using System.Linq; // Required for Any()
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

        /// <summary>
        /// Genera una secuencia de bytes de comandos ESC/POS a partir de un TicketContent.
        /// </summary>
        /// <param name="ticketContent">El objeto TicketContent que contiene la disposición del ticket.</param>
        /// <param name="printerSettings">La configuración de la impresora para aplicar estilos específicos o ancho de papel.</param>
        /// <returns>Una matriz de bytes que representa los comandos ESC/POS.</returns>
        public Task<byte[]> GenerateEscPosCommandsAsync(TicketContent ticketContent, PrinterSettings printerSettings)
        {
            _logger.LogInfo($"Generando comandos ESC/POS para impresora {printerSettings.PrinterId} (ancho: {printerSettings.PaperWidthMm}mm).");

            var commands = new List<byte>();

            // Inicializar impresora
            commands.AddRange(InitializePrinter());

            // --- HEADER ---
            // Los elementos de la cabecera pueden tener su propia alineación/formato
            foreach (var element in ticketContent.HeaderElements)
            {
                commands.AddRange(ProcessRenderedElement(element, printerSettings));
            }

            // Separador (adaptado al ancho del papel)
            commands.AddRange(SetAlignment("Center"));
            commands.AddRange(Encoding.UTF8.GetBytes(new string('-', GetCharsPerLine(printerSettings.PaperWidthMm))));
            commands.Add(0x0A); // LF

            // --- ITEMS TABLE ---
            // Alineación de la tabla
            commands.AddRange(SetAlignment("Left"));

            int charsPerLine = GetCharsPerLine(printerSettings.PaperWidthMm);
            foreach (var row in ticketContent.ItemTableRows)
            {
                if (ticketContent.DocumentType == "ticket_venta" && row.Count == 4) // Ahora 4 columnas
                {
                    string colProducto = row[0].TextValue ?? string.Empty;
                    string colCantidad = row[1].TextValue ?? string.Empty;
                    string colPrecioUnitario = row[2].TextValue ?? string.Empty; // Nueva columna
                    string colTotal = row[3].TextValue ?? string.Empty; // Columna ajustada a índice 3

                    // Calcular anchos de columna aproximados
                    // Producto: 40% del ancho, Cantidad: 15%, P.Unitario: 20%, Total: 25%
                    int widthColProducto = (int)(charsPerLine * 0.40);
                    int widthColCantidad = (int)(charsPerLine * 0.15);
                    int widthColPrecioUnitario = (int)(charsPerLine * 0.20);
                    int widthColTotal = (int)(charsPerLine * 0.25);

                    colProducto = colProducto.Length > widthColProducto ? colProducto.Substring(0, widthColProducto) : colProducto;
                    colCantidad = colCantidad.Length > widthColCantidad ? colCantidad.Substring(0, widthColCantidad) : colCantidad;
                    colPrecioUnitario = colPrecioUnitario.Length > widthColPrecioUnitario ? colPrecioUnitario.Substring(0, widthColPrecioUnitario) : colPrecioUnitario;
                    colTotal = colTotal.Length > widthColTotal ? colTotal.Substring(0, widthColTotal) : colTotal;

                    string formattedLine = $"{colProducto.PadRight(widthColProducto)}{colCantidad.PadLeft(widthColCantidad)}{colPrecioUnitario.PadLeft(widthColPrecioUnitario)}{colTotal.PadLeft(widthColTotal)}";
                    
                    commands.AddRange(Encoding.UTF8.GetBytes(formattedLine));
                    commands.Add(0x0A); // LF
                }
                else if (ticketContent.DocumentType == "comanda" && row.Count >= 2) // Para comanda, procesamos solo Producto y Cantidad
                {
                    string colProducto = row[0].TextValue ?? string.Empty;
                    string colCantidad = row[1].TextValue ?? string.Empty;

                    // Calcular anchos de columna aproximados
                    // Producto: 70% del ancho, Cantidad: 30%
                    int widthColProducto = (int)(charsPerLine * 0.70);
                    int widthColCantidad = (int)(charsPerLine * 0.30);

                    colProducto = colProducto.Length > widthColProducto ? colProducto.Substring(0, widthColProducto) : colProducto;
                    colCantidad = colCantidad.Length > widthColCantidad ? colCantidad.Substring(0, widthColCantidad) : colCantidad;

                    string formattedLine = $"{colProducto.PadRight(widthColProducto)}{colCantidad.PadLeft(widthColCantidad)}";
                    
                    commands.AddRange(Encoding.UTF8.GetBytes(formattedLine));
                    commands.Add(0x0A); // LF
                }
                else // Fallback para otras configuraciones de documento o conteo de columnas inesperado
                {
                    foreach (var element in row)
                    {
                        commands.AddRange(ProcessRenderedElement(element, printerSettings));
                    }
                }
            }

            // Separador
            commands.AddRange(SetAlignment("Center"));
            commands.AddRange(Encoding.UTF8.GetBytes(new string('-', GetCharsPerLine(printerSettings.PaperWidthMm))));
            commands.Add(0x0A); // LF

            // --- DETAILS (para comandas) ---
            
                commands.Add(0x0A); // Salto de línea antes del detalle
                commands.AddRange(SetAlignment("Left")); // Alineación para el detalle

                foreach (var element in ticketContent.Details)
                {
                    commands.AddRange(ProcessRenderedElement(element, printerSettings));
                }
                commands.Add(0x0A); // Salto de línea después del detalle
            

            // --- TOTALS ---
            // Los elementos de totales pueden tener su propia alineación/formato
            foreach (var element in ticketContent.TotalsElements)
            {
                commands.AddRange(ProcessRenderedElement(element, printerSettings));
            }

                // Separador
                commands.AddRange(SetAlignment("Center"));
                commands.AddRange(Encoding.UTF8.GetBytes(new string('-', GetCharsPerLine(printerSettings.PaperWidthMm))));
                commands.Add(0x0A); // LF
            

            // --- FOOTER ---
            // Los elementos del pie de página pueden tener su propia alineación/formato
            foreach (var element in ticketContent.FooterElements)
            {
                commands.AddRange(ProcessRenderedElement(element, printerSettings));
            }

            // --- REPEATED CONTENT (e.g., Barcode Stickers) ---
            if (ticketContent.RepeatedContent.Any())
            {
                _logger.LogInfo($"Procesando contenido repetitivo con {ticketContent.RepeatedContentColumns} columna(s).");
                commands.Add(0x0A); // LF
                commands.AddRange(SetAlignment("Center")); // Alineación para el bloque de contenido repetitivo

                foreach (var rowOfElements in ticketContent.RepeatedContent)
                {
                    // Si la fila contiene solo un elemento con un separador, lo imprimimos directamente.
                    if (rowOfElements.Count == 1 && rowOfElements[0].Type == "Text" && rowOfElements[0].TextValue == "----------")
                    {
                        commands.AddRange(Encoding.UTF8.GetBytes(rowOfElements[0].TextValue));
                        commands.Add(0x0A); // LF
                        continue;
                    }

                    // En una implementación real para múltiples columnas por fila, se necesitaría un cálculo
                    // más sofisticado para posicionar los elementos (ej., ESC a n x y), o dividir la línea.
                    // Para ESC/POS, esto es complejo ya que no soporta layout de columnas fácil.
                    // Aquí, por simplicidad, cada elemento de la fila se procesa secuencialmente.
                    // La alineación dentro de cada "columna" virtual la dará el RenderedElement individual.
                    foreach (var element in rowOfElements)
                    {
                        commands.AddRange(ProcessRenderedElement(element, printerSettings));
                    }
                    commands.Add(0x0A); // Salto de línea después de cada "fila lógica" de RepeatedContent
                }
            }


            // Alimentar un poco de papel
            commands.AddRange(FeedLines(5));

            // Si está configurado para abrir el cajón sin imprimir (ej. antes de la impresión)
            if (printerSettings.OpenCashDrawerWithoutPrint)
            {
                commands.AddRange(OpenCashDrawer());
            }

            // Si está configurado para beep al imprimir
            if (printerSettings.BeepOnPrint)
            {
                commands.AddRange(GenerateBeep());
            }

            // Cortar papel si está configurado
            commands.AddRange(CutPaper());

            // Abrir cajón monedero si está configurado para hacerlo después de la impresión
            if (printerSettings.OpenCashDrawerAfterPrint)
            {
                commands.AddRange(OpenCashDrawer());
            }

            _logger.LogInfo($"Generación de comandos ESC/POS completada. Total bytes: {commands.Count}");
            return Task.FromResult(commands.ToArray());
        }

        // --- Comandos ESC/POS genéricos ---

        private byte[] InitializePrinter()
        {
            return new byte[] { ESC, 0x40 }; // ESC @ - Inicializa la impresora
        }

        private byte[] SetAlignment(string align)
        {
            byte n = 0x00; // Left
            if (align?.Equals("Center", StringComparison.OrdinalIgnoreCase) == true)
                n = 0x01; // Center
            else if (align?.Equals("Right", StringComparison.OrdinalIgnoreCase) == true)
                n = 0x02; // Right
            return new byte[] { ESC, 0x61, n }; // ESC a n
        }

        private byte[] SetTextFormat(string format)
        {
            var cmd = new List<byte>();
            byte fontStyle = 0x00; // Normal
            byte fontSize = 0x00;  // 1x1

            if (format != null)
            {
                if (format.Contains("Bold", StringComparison.OrdinalIgnoreCase))
                {
                    cmd.Add(ESC); cmd.Add(0x45); cmd.Add(0x01); // ESC E 1 - Activar negrita
                }
                if (format.Contains("Large", StringComparison.OrdinalIgnoreCase) || format.Contains("DoubleHeight", StringComparison.OrdinalIgnoreCase))
                {
                    fontSize |= 0x01; // Doble altura
                }
                if (format.Contains("DoubleWidth", StringComparison.OrdinalIgnoreCase))
                {
                    fontSize |= 0x10; // Doble ancho
                }
                if (format.Contains("Underline", StringComparison.OrdinalIgnoreCase))
                {
                    cmd.Add(ESC); cmd.Add(0x2D); cmd.Add(0x01); // ESC - 1 - Subrayado simple
                }
            }

            if (fontSize != 0x00)
            {
                cmd.Add(GS); cmd.Add(0x21); cmd.Add(fontSize); // GS ! n - Seleccionar tamaño de carácter
            }
            
            return cmd.ToArray();
        }

        private byte[] ResetTextFormat()
        {
            // Desactivar todos los formatos
            var cmd = new List<byte>();
            cmd.Add(ESC); cmd.Add(0x45); cmd.Add(0x00); // ESC E 0 - Desactivar negrita
            cmd.Add(ESC); cmd.Add(0x2D); cmd.Add(0x00); // ESC - 0 - Desactivar subrayado
            cmd.Add(GS); cmd.Add(0x21); cmd.Add(0x00); // GS ! 0 - Tamaño de fuente normal (1x1)
            return cmd.ToArray();
        }


        private byte[] FeedLines(int lines)
        {
            if (lines < 0 || lines > 255) lines = 3; // Valor por defecto
            return new byte[] { ESC, 0x64, (byte)lines }; // ESC d n - Imprime n líneas en blanco
        }

        private byte[] CutPaper()
        {
            return new byte[] { GS, 0x56, 0x00 }; // GS V 0 - Corte completo
        }

        private byte[] OpenCashDrawer()
        {
            // Comandos comunes para abrir el cajón monedero. Puede variar según la impresora.
            return new byte[] { ESC, 0x70, 0x00, 0x32, 0x32 }; // ESC p 0 m t1 t2
        }

        // --- Procesamiento de RenderedElement ---
        private byte[] ProcessRenderedElement(RenderedElement element, PrinterSettings printerSettings)
        {
            var elementCommands = new List<byte>();

            // Aplicar alineación si está especificada
            elementCommands.AddRange(SetAlignment(element.Align));
            // Aplicar formato de texto si está especificado
            elementCommands.AddRange(SetTextFormat(element.Format));

            switch (element.Type)
            {
                case "Text":
                    if (!string.IsNullOrEmpty(element.TextValue))
                    {
                        elementCommands.AddRange(Encoding.UTF8.GetBytes(element.TextValue));
                        elementCommands.Add(0x0A); // LF después de cada línea de texto
                    }
                    break;
                case "Image":
                    // REQUIERE IMPLEMENTACIÓN ADICIONAL:
                    // Convertir element.Base64Image a bitmap y luego a formato raster ESC/POS.
                    // Esto usualmente necesita una librería de procesamiento de imágenes como System.Drawing.Common
                    // o ImageSharp, y lógica compleja para dithering y compresión.
                    _logger.LogWarning("La generación de imágenes ESC/POS no está implementada. Ignorando imagen.");
                    elementCommands.AddRange(Encoding.UTF8.GetBytes("--- IMAGEN NO SOPORTADA ---"));
                    elementCommands.Add(0x0A);
                    break;
                case "Barcode":
                    elementCommands.AddRange(HandleBarcode(element, printerSettings));
                    break;
                case "QR":
                    elementCommands.AddRange(HandleQR(element, printerSettings));
                    break;
                default:
                    _logger.LogWarning($"Tipo de elemento desconocido: {element.Type}. Ignorando.");
                    break;
            }

            // Resetear formato de texto después de cada elemento para evitar que afecte al siguiente
            elementCommands.AddRange(ResetTextFormat());

            return elementCommands.ToArray();
        }

        /// <summary>
        /// Calcula el número aproximado de caracteres por línea basado en el ancho del papel.
        /// Asume un tamaño de fuente estándar. Para un control preciso, se necesitarían
        /// comandos ESC/POS para establecer el tamaño de fuente.
        /// </summary>
        /// <param name="paperWidthMm">Ancho del papel en milímetros (ej. 58 o 80).</param>
        /// <returns>Número de caracteres por línea.</returns>
        private int GetCharsPerLine(int paperWidthMm)
        {
            // Estimaciones comunes para fuentes de 80mm y 58mm en impresoras POS
            if (paperWidthMm >= 80)
            {
                return 48; // Típicamente 48 caracteres para 80mm
            }
            else // Asumir 58mm o menor
            {
                return 32; // Típicamente 32 caracteres para 58mm
            }
        }
        private byte[] GenerateBeep()
        {
            // GS ( E 04 00 01 01 m n
            // m=1 (buzzer), n=1 (beep)
            return new byte[] { GS, 0x28, 0x45, 0x04, 0x00, 0x01, 0x01, 0x01, 0x01 }; // Genera un beep
        }

        private byte[] HandleBarcode(RenderedElement element, PrinterSettings printerSettings)
        {
            var commands = new List<byte>();

            // Configuración general del código de barras
            // commands.AddRange(new byte[] { GS, 0x77, 0x02 }); // GS w n - Ancho del módulo (2 puntos)
            commands.AddRange(new byte[] { GS, 0x68, (byte)(element.Height ?? 50) }); // GS h n - Alto del código de barras (default 50)
            commands.AddRange(new byte[] { GS, 0x48, (byte)(element.Hri == true ? 0x02 : 0x00) }); // GS H n - Posición HRI (0: none, 1: above, 2: below, 3: both)

            byte barcodeType = 0; // Por defecto para CODE128 o auto
            byte[] barcodeData = Encoding.UTF8.GetBytes(element.BarcodeValue);

            switch (element.BarcodeType?.ToUpper())
            {
                case "EAN13":
                    if (element.BarcodeValue.Length != 12)
                    {
                        _logger.LogWarning($"EAN13 requiere 12 dígitos de datos, recibido {element.BarcodeValue.Length}.");
                        return Encoding.UTF8.GetBytes($"EAN13_ERROR: {element.BarcodeValue}");
                    }
                    barcodeType = 0x43; // EAN13
                    commands.AddRange(new byte[] { GS, 0x6B, barcodeType, (byte)barcodeData.Length });
                    commands.AddRange(barcodeData);
                    break;
                case "CODE128":
                    barcodeType = 0x49; // CODE128
                    commands.AddRange(new byte[] { GS, 0x6B, barcodeType, (byte)(barcodeData.Length + 2) }); // +2 para {B y el NULL final
                    commands.Add(0x7B); // ASCII { (switch to Code B)
                    commands.AddRange(barcodeData);
                    break;
                default:
                    _logger.LogWarning($"Tipo de código de barras '{element.BarcodeType}' no soportado o inválido. Intentando CODE128.");
                    barcodeType = 0x49; // CODE128 como fallback
                    commands.AddRange(new byte[] { GS, 0x6B, barcodeType, (byte)(barcodeData.Length + 2) });
                    commands.Add(0x7B); // ASCII { (switch to Code B)
                    commands.AddRange(barcodeData);
                    break;
            }
            commands.Add(0x0A); // Salto de línea después del código de barras
            return commands.ToArray();
        }

        private byte[] HandleQR(RenderedElement element, PrinterSettings printerSettings)
        {
            var commands = new List<byte>();
            byte[] qrData = Encoding.UTF8.GetBytes(element.QrValue);

            // Modelo QR: GS ( k pL pH cn 48 n1
            // n1 = 1 (Modelo 1) o 2 (Modelo 2)
            commands.AddRange(new byte[] { GS, 0x28, 0x6B, 0x04, 0x00, 0x31, 0x41, 0x32, 0x00 }); // Function 165 (QR Code: Select model) -> Set Model 2

            // Tamaño del módulo: GS ( k pL pH cn 48 n1
            // n1 = 3-16 (default 3)
            byte size = (byte)(element.Size.GetValueOrDefault(3) > 16 ? 16 : element.Size.GetValueOrDefault(3) < 1 ? 1 : element.Size.GetValueOrDefault(3));
            commands.AddRange(new byte[] { GS, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x43, size }); // Function 167 (QR Code: Set size of module)

            // Nivel de corrección de error: GS ( k pL pH cn 48 n1
            // n1 = 49 (L), 50 (M), 51 (Q), 52 (H)
            commands.AddRange(new byte[] { GS, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x45, 0x32 }); // Function 169 (QR Code: Set error correction level) -> Level M

            // Almacenar datos: GS ( k pL pH cn 49 d1...dk
            int dataLength = qrData.Length;
            byte pL = (byte)(dataLength % 256);
            byte pH = (byte)(dataLength / 256);
            commands.AddRange(new byte[] { GS, 0x28, 0x6B, pL, pH, 0x31, 0x50, 0x30 }); // Function 180 (QR Code: Store data)
            commands.AddRange(qrData);

            // Imprimir símbolo: GS ( k pL pH cn 49
            commands.AddRange(new byte[] { GS, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x51, 0x30 }); // Function 181 (QR Code: Print symbol)

            commands.Add(0x0A); // Salto de línea después del QR
            return commands.ToArray();
        }
    }
}
