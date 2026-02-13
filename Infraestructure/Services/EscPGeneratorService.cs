using AppsielPrintManager.Core.Interfaces;
using AppsielPrintManager.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppsielPrintManager.Infraestructure.Services
{
    /// <summary>
    /// Generador de comandos ESC/P para impresoras matriciales (Epson Serie LX, LQ, FX).
    /// </summary>
    public class EscPGeneratorService
    {
        private readonly ILoggingService _logger;
        private readonly Encoding _encoding;

        private const byte ESC = 0x1B;
        private const byte GS = 0x1D;

        public EscPGeneratorService(ILoggingService logger)
        {
            _logger = logger;
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            try { _encoding = Encoding.GetEncoding(850); }
            catch { _encoding = Encoding.ASCII; }
        }

        public Task<byte[]> GenerateEscPCommandsAsync(string gridContent, PrinterSettings settings)
        {
            var commands = new List<byte>();

            // 1. Inicializar Impresora (ESC @)
            commands.Add(ESC);
            commands.Add(0x40);

            // 2. Establecer set de caracteres (Espanol - ESC R 7)
            commands.Add(ESC);
            commands.Add(0x52);
            commands.Add(0x07);

            // 3. Draft Mode (Velocidad maxima - ESC x 0)
            commands.Add(ESC);
            commands.Add(0x78);
            commands.Add(0x00);

            // 4. Activar Modo Condensado (SI - 0x0F) si el ancho es > 80
            // Analizamos la primera linea para determinar el ancho real de la grilla
            var firstLine = gridContent.Split('\n').FirstOrDefault();
            bool useCondensed = firstLine != null && firstLine.TrimEnd('\r').Length > 80;

            if (useCondensed)
            {
                _logger.LogInfo("Ancho de grilla > 80. Activando Modo Condensado (SI).");
                commands.Add(0x0F);
            }

            // 5. Convertir el contenido de la grilla a bytes
            byte[] body = _encoding.GetBytes(gridContent);
            commands.AddRange(body);

            // 6. Desactivar Modo Condensado (DC2 - 0x12) al terminar si se activo
            if (useCondensed)
            {
                commands.Add(0x12);
            }

            // 7. Salto de pagina (Form Feed - FF)
            commands.Add(0x0C);

            return Task.FromResult(commands.ToArray());
        }

        /// <summary>
        /// Comandos utiles para el futuro (Condensed, Bold, etc).
        /// </summary>
        public static class Commands
        {
            public static byte[] CondensedOn = new byte[] { 0x0F };
            public static byte[] CondensedOff = new byte[] { 0x12 };
            public static byte[] BoldOn = new byte[] { 0x1B, 0x45 };
            public static byte[] BoldOff = new byte[] { 0x1B, 0x46 };
            public static byte[] ItalicsOn = new byte[] { 0x1B, 0x34 };
            public static byte[] ItalicsOff = new byte[] { 0x1B, 0x35 };
        }
    }
}
