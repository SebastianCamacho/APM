using AppsielPrintManager.Core.Interfaces;
using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace AppsielPrintManager.Infraestructure.Services
{
    /// <summary>
    /// Cliente para enviar comandos ESC/POS a una impresora térmica a través de TCP/IP.
    /// Esta clase gestiona la conexión y el envío de datos.
    /// </summary>
    public class TcpIpPrinterClient
    {
        private readonly ILoggingService _logger;

        public TcpIpPrinterClient(ILoggingService logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Envía un arreglo de bytes (comandos ESC/POS) a una impresora TCP/IP.
        /// Abre una conexión, envía los datos y cierra la conexión.
        /// </summary>
        /// <param name="ipAddress">La dirección IP de la impresora.</param>
        /// <param name="port">El puerto TCP de la impresora (ej. 9100).</param>
        /// <param name="data">Los bytes de comandos ESC/POS a enviar.</param>
        /// <returns>True si la impresión fue exitosa, false en caso contrario.</returns>
        public async Task<bool> PrintAsync(string ipAddress, int port, byte[] data)
        {
            if (string.IsNullOrWhiteSpace(ipAddress))
            {
                _logger.LogError("La dirección IP de la impresora no puede ser nula o vacía.");
                return false;
            }
            if (port <= 0)
            {
                _logger.LogError("El puerto de la impresora debe ser un número positivo.");
                return false;
            }
            if (data == null || data.Length == 0)
            {
                _logger.LogWarning($"No hay datos para enviar a la impresora {ipAddress}:{port}.");
                return true; // Considerar como éxito si no hay datos para imprimir, o como error si se espera algo.
            }

            using (var client = new TcpClient())
            {
                try
                {
                    _logger.LogInfo($"Intentando conectar a la impresora en {ipAddress}:{port}...");
                    await client.ConnectAsync(ipAddress, port);
                    _logger.LogInfo($"Conectado exitosamente a la impresora en {ipAddress}:{port}.");

                    using (var stream = client.GetStream())
                    {
                        await stream.WriteAsync(data, 0, data.Length);
                        _logger.LogInfo($"Datos enviados a la impresora en {ipAddress}:{port}. Bytes enviados: {data.Length}");
                    }
                    return true;
                }
                catch (SocketException sex)
                {
                    _logger.LogError($"Error de socket al imprimir en {ipAddress}:{port}: {sex.Message}. Código de error: {sex.ErrorCode}", sex);
                    return false;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error inesperado al imprimir en {ipAddress}:{port}: {ex.Message}", ex);
                    return false;
                }
                finally
                {
                    client.Close();
                    _logger.LogInfo($"Conexión cerrada con la impresora en {ipAddress}:{port}.");
                }
            }
        }
    }
}
