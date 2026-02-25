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
        /// <summary>
        /// Consulta el estado físico de la impresora (si está en línea y si tiene papel).
        /// </summary>
        public async Task<(bool IsOnline, string StatusMessage)> GetPrinterStatusAsync(string ipAddress, int port)
        {
            using (var client = new TcpClient())
            {
                using var cts = new CancellationTokenSource(3000);
                try
                {
                    // 1. Verificar si está encendida/en red
                    var connectTask = client.ConnectAsync(ipAddress, port, cts.Token);
                    await connectTask;

                    if (!client.Connected) return (false, "No se pudo establecer conexión con la impresora.");

                    using (var stream = client.GetStream())
                    {
                        stream.ReadTimeout = 1500;
                        stream.WriteTimeout = 1500;

                        // 2. Pedir estado Off-line: DLE EOT 2 (0x10 0x04 0x02)
                        byte[] statusCmd2 = new byte[] { 0x10, 0x04, 0x02 };
                        await stream.WriteAsync(statusCmd2, 0, statusCmd2.Length, cts.Token);

                        byte[] buffer = new byte[1];
                        int read2 = await stream.ReadAsync(buffer, 0, 1, cts.Token);
                        if (read2 > 0 && (buffer[0] & 0x04) != 0)
                        {
                            return (false, "La tapa de la impresora está abierta.");
                        }

                        // 3. Pedir estado del papel: DLE EOT 4 (0x10 0x04 0x04)
                        byte[] statusCmd4 = new byte[] { 0x10, 0x04, 0x04 };
                        await stream.WriteAsync(statusCmd4, 0, statusCmd4.Length, cts.Token);

                        int read4 = await stream.ReadAsync(buffer, 0, 1, cts.Token);
                        if (read4 > 0 && (buffer[0] & 0x60) != 0)
                        {
                            return (false, "La impresora no tiene papel.");
                        }
                    }

                    return (true, "OK");
                }
                catch (OperationCanceledException)
                {
                    return (false, "Impresora fuera de línea o apagada (Timeout de conexión)");
                }
                catch (SocketException sex) when (sex.SocketErrorCode == SocketError.ConnectionRefused)
                {
                    // Algunas impresoras cierran el puerto TCP totalmente cuando la tapa está abierta o no hay papel
                    _logger.LogWarning($"La impresora en {ipAddress}:{port} rechazó la conexión. Esto suele indicar Tapa Abierta, Falta de Papel o Error Crítico.");
                    return (false, "La impresora rechazó la conexión. Verifica si la tapa está abierta o si no tiene papel.");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"No se pudo obtener el estado de la impresora {ipAddress}: {ex.Message}");
                    return (false, $"Error de comunicación: {ex.Message}");
                }
            }
        }
    }
}
