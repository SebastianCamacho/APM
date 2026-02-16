using System;
using System.Net.Http;
using System.Threading.Tasks;
using AppsielPrintManager.Core.Interfaces;

namespace AppsielPrintManager.Infraestructure.Services
{
    /// <summary>
    /// Cliente para enviar datos de impresión a través de IPP (Internet Printing Protocol) 
    /// o servidores de impresión basados en HTTP.
    /// </summary>
    public class IppPrinterClient
    {
        private readonly ILoggingService _logger;
        private readonly HttpClient _httpClient;

        public IppPrinterClient(ILoggingService logger)
        {
            _logger = logger;
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        /// <summary>
        /// Envía los comandos de impresión a una URI IPP/HTTP.
        /// </summary>
        /// <param name="uri">La URI completa del destino (ej: http://192.168.1.50/ipp/print).</param>
        /// <param name="data">Los bytes de comandos (ESC/POS o ESC/P).</param>
        /// <returns>True si el envío fue exitoso.</returns>
        public async Task<bool> PrintAsync(string uri, byte[] data)
        {
            if (string.IsNullOrEmpty(uri))
            {
                _logger.LogError("Error IPP: La URI de la impresora está vacía.");
                return false;
            }

            // Convertir esquema ipp:// a http:// si es necesario para HttpClient
            string targetUrl = uri.Replace("ipp://", "http://", StringComparison.OrdinalIgnoreCase)
                                 .Replace("ipps://", "https://", StringComparison.OrdinalIgnoreCase);

            try
            {
                _logger.LogInfo($"Enviando datos IPP a: {targetUrl} ({data.Length} bytes)");

                using (var content = new ByteArrayContent(data))
                {
                    content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

                    var response = await _httpClient.PostAsync(targetUrl, content);

                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogInfo($"Envío IPP exitoso a {targetUrl}");
                        return true;
                    }
                    else
                    {
                        string errorDetail = await response.Content.ReadAsStringAsync();
                        _logger.LogError($"Error IPP ({response.StatusCode}): {errorDetail}");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Excepción enviando datos IPP a {targetUrl}: {ex.Message}", ex);
                return false;
            }
        }
    }
}
