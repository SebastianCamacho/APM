using AppsielPrintManager.Core.Interfaces;
using AppsielPrintManager.Core.Models;
using System.Text.Json; // Agregado para JsonSerializer
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace AppsielPrintManager.Infraestructure.Services
{
    /// <summary>
    /// Implementación de IPrintService, orquestando el proceso completo
    /// desde la recepción de la solicitud hasta el envío a la impresora(s).
    /// </summary>
    public class PrintService : IPrintService
    {
        private readonly ILoggingService _logger;
        private readonly ISettingsRepository _settingsRepository;
        private readonly ITicketRenderer _ticketRenderer;
        private readonly IEscPosGenerator _escPosGenerator;
        private readonly TcpIpPrinterClient _tcpIpPrinterClient;

        public PrintService(ILoggingService logger,
                            ISettingsRepository settingsRepository,
                            ITicketRenderer ticketRenderer,
                            IEscPosGenerator escPosGenerator,
                            TcpIpPrinterClient tcpIpPrinterClient)
        {
            _logger = logger;
            _settingsRepository = settingsRepository;
            _ticketRenderer = ticketRenderer;
            _escPosGenerator = escPosGenerator;
            _tcpIpPrinterClient = tcpIpPrinterClient;
        }

        /// <summary>
        /// Procesa una solicitud de trabajo de impresión.
        /// Incluye parseo, renderizado, generación de comandos ESC/POS y envío a la impresora(s).
        /// </summary>
        /// <param name="request">El objeto PrintJobRequest que contiene los detalles del trabajo de impresión.</param>
        /// <returns>Un PrintJobResult indicando el éxito o fracaso de la operación.</returns>
        public async Task<PrintJobResult> ProcessPrintJobAsync(PrintJobRequest request)
        {
            _logger.LogInfo($"Iniciando procesamiento de trabajo de impresión para JobId: {request.JobId}");
            var result = new PrintJobResult { JobId = request.JobId, Status = "ERROR" };

            try
            {
                // 1. Obtener configuración de la impresora principal
                var printerSettings = await _settingsRepository.GetPrinterSettingsAsync(request.PrinterId);
                if (printerSettings == null)
                {
                    result.ErrorMessage = $"Impresora con ID '{request.PrinterId}' no encontrada o no configurada.";
                    _logger.LogError(result.ErrorMessage);
                    return result;
                }

                // 2. Deserializar el documento
                object documentData = null;
                try
                {
                    var jsonDocument = JsonSerializer.Serialize(request.Document); // request.Document es un objeto, lo serializamos a string para luego deserializarlo al tipo correcto
                    switch (request.DocumentType)
                    {
                        case "ticket_venta":
                            documentData = JsonSerializer.Deserialize<SaleTicketDocumentData>(jsonDocument, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                            break;
                        case "comanda":
                            documentData = JsonSerializer.Deserialize<CommandDocumentData>(jsonDocument, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                            break;
                        case "factura_electronica":
                            documentData = JsonSerializer.Deserialize<ElectronicInvoiceDocumentData>(jsonDocument, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                            break;
                        case "sticker_codigo_barras":
                            documentData = JsonSerializer.Deserialize<BarcodeStickerDocumentData>(jsonDocument, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                            break;
                        default:
                            result.ErrorMessage = $"DocumentType '{request.DocumentType}' no soportado para deserialización.";
                            _logger.LogError(result.ErrorMessage);
                            return result;
                    }

                    if (documentData == null)
                    {
                        result.ErrorMessage = $"Error al deserializar el documento para DocumentType '{request.DocumentType}'.";
                        _logger.LogError(result.ErrorMessage);
                        return result;
                    }
                }
                catch (JsonException jex)
                {
                    result.ErrorMessage = $"Error de formato JSON al deserializar el documento para DocumentType '{request.DocumentType}': {jex.Message}";
                    _logger.LogError(result.ErrorMessage, jex);
                    return result;
                }
                catch (System.Exception ex)
                {
                    result.ErrorMessage = $"Error inesperado al deserializar el documento para DocumentType '{request.DocumentType}': {ex.Message}";
                    _logger.LogError(result.ErrorMessage, ex);
                    return result;
                }

                // 3. Renderizar el TicketContent (pasando el request original para media y el documento tipado)
                var ticketContent = await _ticketRenderer.RenderTicketAsync(request, documentData);

                // 4. Generar comandos ESC/POS
                var escPosCommands = await _escPosGenerator.GenerateEscPosCommandsAsync(ticketContent, printerSettings);

                // 4. Enviar a la impresora principal
                bool primaryPrintSuccess = await _tcpIpPrinterClient.PrintAsync(printerSettings.IpAddress, printerSettings.Port, escPosCommands);

                if (!primaryPrintSuccess)
                {
                    result.ErrorMessage = $"Fallo al imprimir en la impresora principal '{request.PrinterId}' ({printerSettings.IpAddress}:{printerSettings.Port}).";
                    _logger.LogError(result.ErrorMessage);
                    return result;
                }

                // 5. Enviar a impresoras de copia si existen
                if (printerSettings.CopyToPrinterIds != null && printerSettings.CopyToPrinterIds.Any())
                {
                    foreach (var copyPrinterId in printerSettings.CopyToPrinterIds)
                    {
                        var copyPrinterSettings = await _settingsRepository.GetPrinterSettingsAsync(copyPrinterId);
                        if (copyPrinterSettings == null)
                        {
                            _logger.LogWarning($"Impresora de copia con ID '{copyPrinterId}' no encontrada. Se omite la copia.");
                            continue;
                        }

                        _logger.LogInfo($"Enviando copia a impresora '{copyPrinterId}' ({copyPrinterSettings.IpAddress}:{copyPrinterSettings.Port}).");
                        var copyEscPosCommands = await _escPosGenerator.GenerateEscPosCommandsAsync(ticketContent, copyPrinterSettings); // Regenerar por si la configuración de copia es diferente
                        bool copyPrintSuccess = await _tcpIpPrinterClient.PrintAsync(copyPrinterSettings.IpAddress, copyPrinterSettings.Port, copyEscPosCommands);

                        if (!copyPrintSuccess)
                        {
                            _logger.LogError($"Fallo al imprimir en impresora de copia '{copyPrinterId}'. Continúa el proceso principal.");
                            // No se detiene el proceso principal si falla una copia, solo se registra.
                        }
                    }
                }

                result.Status = "DONE";
                _logger.LogInfo($"Trabajo de impresión '{request.JobId}' completado exitosamente.");
            }
            catch (System.Exception ex)
            {
                result.ErrorMessage = $"Excepción inesperada durante el procesamiento del trabajo de impresión '{request.JobId}': {ex.Message}";
                _logger.LogError(result.ErrorMessage, ex);
            }

            return result;
        }

        /// <summary>
        /// Permite configurar una impresora específica.
        /// </summary>
        /// <param name="settings">La configuración de la impresora a guardar o actualizar.</param>
        /// <returns>Tarea que representa la operación asíncrona.</returns>
        public async Task ConfigurePrinterAsync(PrinterSettings settings)
        {
            await _settingsRepository.SavePrinterSettingsAsync(settings);
            _logger.LogInfo($"Configuración de impresora para '{settings.PrinterId}' guardada/actualizada.");
        }

        /// <summary>
        /// Obtiene la configuración de una impresora por su identificador.
        /// </summary>
        /// <param name="printerId">El identificador único de la impresora.</param>
        /// <returns>La configuración de la impresora si se encuentra, de lo contrario, null.</returns>
        public async Task<PrinterSettings> GetPrinterSettingsAsync(string printerId)
        {
            return await _settingsRepository.GetPrinterSettingsAsync(printerId);
        }

        /// <summary>
        /// Obtiene todas las configuraciones de impresoras guardadas.
        /// </summary>
        /// <returns>Una lista de todas las configuraciones de impresoras.</returns>
        public async Task<List<PrinterSettings>> GetAllPrinterSettingsAsync()
        {
            return await _settingsRepository.GetAllPrinterSettingsAsync();
        }

        /// <summary>
        /// Elimina la configuración de una impresora por su identificador.
        /// </summary>
        /// <param name="printerId">El identificador único de la impresora a eliminar.</param>
        /// <returns>Tarea que representa la operación asíncrona.</returns>
        public async Task DeletePrinterSettingsAsync(string printerId)
        {
            await _settingsRepository.DeletePrinterSettingsAsync(printerId);
            _logger.LogInfo($"Configuración de impresora para '{printerId}' eliminada.");
        }
    }
}
