using AppsielPrintManager.Core.Interfaces;
using AppsielPrintManager.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace AppsielPrintManager.Infraestructure.Repositories
{
    /// <summary>
    /// Implementación de ISettingsRepository que persiste las configuraciones de impresora
    /// en un archivo JSON local.
    /// </summary>
    public class SettingsRepository : ISettingsRepository
    {
        private readonly ILoggingService _logger;
        private readonly string _filePath;
        private const string SettingsFileName = "printersettings.json";

        /// <summary>
        /// Constructor del repositorio de configuraciones.
        /// Inicializa la ruta del archivo de persistencia.
        /// </summary>
        /// <param name="logger">Servicio de logging para registrar eventos.</param>
        public SettingsRepository(ILoggingService logger)
        {
            _logger = logger;

            // Usar siempre LocalApplicationData para consistencia entre UI y WorkerService
            // y evitar problemas de dependencias de compilación con #if WINDOWS
            string appDataDirectory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            // Crear una subcarpeta específica para nuestra aplicación
            string appSpecificDirectory = Path.Combine(appDataDirectory, "AppsielPrintManager");

            // Asegurarse de que el directorio exista
            if (!Directory.Exists(appSpecificDirectory))
            {
                Directory.CreateDirectory(appSpecificDirectory);
            }

            _filePath = Path.Combine(appSpecificDirectory, SettingsFileName);
            _filePath = Path.Combine(appSpecificDirectory, SettingsFileName);
            _logger.LogInfo($"Ruta del archivo de configuraciones de impresora: {_filePath}");
        }

        /// <summary>
        /// Guarda o actualiza la configuración de una impresora.
        /// Si la impresora ya existe (mismo PrinterId), se actualiza. De lo contrario, se añade.
        /// </summary>
        /// <param name="settings">La configuración de la impresora a guardar.</param>
        /// <returns>Tarea que representa la operación asíncrona.</returns>
        public async Task SavePrinterSettingsAsync(PrinterSettings settings)
        {
            if (string.IsNullOrWhiteSpace(settings.PrinterId))
            {
                _logger.LogError("No se puede guardar la configuración de la impresora: PrinterId es nulo o vacío.");
                throw new ArgumentException("PrinterId no puede ser nulo o vacío.", nameof(settings.PrinterId));
            }

            var allSettings = await LoadAllSettingsAsync();
            var existingSetting = allSettings.FirstOrDefault(s => s.PrinterId == settings.PrinterId);

            if (existingSetting != null)
            {
                // Actualizar la configuración existente
                allSettings.Remove(existingSetting);
                allSettings.Add(settings);
                _logger.LogInfo($"Configuración de impresora actualizada para PrinterId: {settings.PrinterId}");
            }
            else
            {
                // Añadir nueva configuración
                allSettings.Add(settings);
                _logger.LogInfo($"Nueva configuración de impresora guardada para PrinterId: {settings.PrinterId}");
            }

            await WriteAllSettingsAsync(allSettings);
        }

        /// <summary>
        /// Obtiene la configuración de una impresora por su identificador único.
        /// </summary>
        /// <param name="printerId">El identificador de la impresora.</param>
        /// <returns>La configuración de la impresora si se encuentra, de lo contrario, null.</returns>
        public async Task<PrinterSettings> GetPrinterSettingsAsync(string printerId)
        {
            var allSettings = await LoadAllSettingsAsync();
            return allSettings.FirstOrDefault(s => s.PrinterId == printerId);
        }

        /// <summary>
        /// Obtiene todas las configuraciones de impresoras almacenadas.
        /// </summary>
        /// <returns>Una lista de todas las configuraciones de impresoras.</returns>
        public async Task<List<PrinterSettings>> GetAllPrinterSettingsAsync()
        {
            return await LoadAllSettingsAsync();
        }

        /// <summary>
        /// Elimina la configuración de una impresora por su identificador único.
        /// </summary>
        /// <param name="printerId">El identificador de la impresora a eliminar.</param>
        /// <returns>True si la impresora fue eliminada exitosamente, false si no se encontró.</returns>
        public async Task<bool> DeletePrinterSettingsAsync(string printerId)
        {
            var allSettings = await LoadAllSettingsAsync();
            var removedCount = allSettings.RemoveAll(s => s.PrinterId == printerId);

            if (removedCount > 0)
            {
                await WriteAllSettingsAsync(allSettings);
                _logger.LogInfo($"Configuración de impresora eliminada para PrinterId: {printerId}");
                return true;
            }

            _logger.LogWarning($"No se encontró configuración de impresora para eliminar con PrinterId: {printerId}");
            return false;
        }

        /// <summary>
        /// Carga todas las configuraciones de impresora desde el archivo JSON.
        /// </summary>
        /// <returns>Una lista de configuraciones de impresora.</returns>
        private async Task<List<PrinterSettings>> LoadAllSettingsAsync()
        {
            if (!File.Exists(_filePath))
            {
                // _logger.LogError($"El archivo no existe {_filePath}"); // Reducir ruido si es esperado
                return new List<PrinterSettings>();
            }

            const int maxRetries = 3;
            const int delayMs = 200;

            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    using (var stream = File.Open(_filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var reader = new StreamReader(stream))
                    {
                        var json = await reader.ReadToEndAsync();
                        return JsonSerializer.Deserialize<List<PrinterSettings>>(json) ?? new List<PrinterSettings>();
                    }
                }
                catch (IOException ioEx)
                {
                    // Si el archivo está bloqueado, esperar y reintentar
                    if (i == maxRetries - 1)
                    {
                        _logger.LogError($"Error IO al cargar configuraciones tras {maxRetries} intentos: {ioEx.Message}", ioEx);
                        return new List<PrinterSettings>();
                    }
                    await Task.Delay(delayMs);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error general al cargar configuraciones de impresora desde {_filePath}: {ex.Message}", ex);
                    return new List<PrinterSettings>();
                }
            }
            return new List<PrinterSettings>();
        }

        /// <summary>
        /// Escribe todas las configuraciones de impresora a un archivo JSON.
        /// </summary>
        /// <param name="settings">La lista de configuraciones de impresora a escribir.</param>
        /// <returns>Tarea que representa la operación asíncrona.</returns>
        private async Task WriteAllSettingsAsync(List<PrinterSettings> settings)
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(settings, options);
                await File.WriteAllTextAsync(_filePath, json);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al guardar configuraciones de impresora en {_filePath}: {ex.Message}", ex);
            }
        }
    }
}
