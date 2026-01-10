using AppsielPrintManager.Core.Interfaces;
using AppsielPrintManager.Core.Models;
using System.Threading.Tasks;
using System.Text;
using System.Collections.Generic; // Para List<byte>

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

            // Comando de inicialización (ESC @)
            commands.Add(0x1B);
            commands.Add(0x40);

            // Intentar alinear al centro para el encabezado (ESC a 1)
            commands.Add(0x1B);
            commands.Add(0x61);
            commands.Add(0x01); // Center alignment

            // Procesar HeaderElements
            foreach (var element in ticketContent.HeaderElements)
            {
                if (element is string text)
                {
                    commands.AddRange(Encoding.ASCII.GetBytes(text));
                    commands.Add(0x0A); // LF
                }
            }

            // Separador
            commands.AddRange(Encoding.ASCII.GetBytes(new string('-', 48))); // Asumiendo 32 caracteres de ancho
            commands.Add(0x0A); // LF

            // Procesar ItemTableRows
            // Alineación a la izquierda para los ítems (ESC a 0)
            commands.Add(0x1B);
            commands.Add(0x61);
            commands.Add(0x00); // Left alignment
            foreach (var row in ticketContent.ItemTableRows)
            {
                // Unir los elementos de la fila con un tabulador en lugar de espacios
                // Cambio solicitado: usar '\t' para separar columnas
                string rowText = string.Join("\t", row);
                commands.AddRange(Encoding.ASCII.GetBytes(rowText));
                commands.Add(0x0A); // LF
            }

            // Separador
            commands.AddRange(Encoding.ASCII.GetBytes(new string('-', 48))); // Asumiendo 32 caracteres de ancho
            commands.Add(0x0A); // LF

            // Procesar TotalsElements
            // Alineación a la derecha para totales (ESC a 2)
            commands.Add(0x1B);
            commands.Add(0x61);
            commands.Add(0x02); // Right alignment
            foreach (var element in ticketContent.TotalsElements)
            {
                if (element is string text)
                {
                    commands.AddRange(Encoding.ASCII.GetBytes(text));
                    commands.Add(0x0A); // LF
                }
            }

            // Separador
            commands.AddRange(Encoding.ASCII.GetBytes(new string('-', 48))); // Asumiendo 32 caracteres de ancho
            commands.Add(0x0A); // LF

            // Procesar FooterElements
            // Alineación al centro para el pie de página (ESC a 1)
            commands.Add(0x1B);
            commands.Add(0x61);
            commands.Add(0x01); // Center alignment
            foreach (var element in ticketContent.FooterElements)
            {
                if (element is string text)
                {
                    commands.AddRange(Encoding.ASCII.GetBytes(text));
                    commands.Add(0x0A); // LF
                }
            }

            // Alimentar un poco de papel (LF x 3)
            commands.Add(0x0A);
            commands.Add(0x0A);
            commands.Add(0x0A);

            // Comando de corte de papel (GS V 0)
            commands.Add(0x1D);
            commands.Add(0x56);
            commands.Add(0x00); // Full cut

            _logger.LogInfo($"Generación de comandos ESC/POS completada. Total bytes: {commands.Count}");
            return Task.FromResult(commands.ToArray());
        }
    }
}
