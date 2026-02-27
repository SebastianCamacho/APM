using AppsielPrintManager.Core.Interfaces;
using AppsielPrintManager.Core.Models;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace AppsielPrintManager.Infraestructure.Repositories
{
    public class AppConfigRepository : IAppConfigRepository
    {
        private readonly ILoggingService _logger;
        private readonly string _filePath;
        private const string ConfigFileName = "appconfig.json";

        public AppConfigRepository(ILoggingService logger)
        {
            _logger = logger;
            string appDataDirectory = OperatingSystem.IsWindows()
                ? Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)
                : Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            string appSpecificDirectory = Path.Combine(appDataDirectory, "AppsielPrintManager");

            if (!Directory.Exists(appSpecificDirectory))
            {
                Directory.CreateDirectory(appSpecificDirectory);
            }

            _filePath = Path.Combine(appSpecificDirectory, ConfigFileName);
            _logger.LogInfo($"Ruta de configuración global de app: {_filePath}", "AppConfigRepository");
        }

        public async Task<AppConfig> GetConfigAsync()
        {
            if (!File.Exists(_filePath))
            {
                return new AppConfig();
            }

            try
            {
                using (var stream = File.Open(_filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var reader = new StreamReader(stream))
                {
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var json = await reader.ReadToEndAsync();
                    return JsonSerializer.Deserialize<AppConfig>(json, options) ?? new AppConfig();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error cargando configuración global desde {_filePath}: {ex.Message}", ex, "AppConfigRepository");
                return new AppConfig();
            }
        }

        public async Task SaveConfigAsync(AppConfig config)
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(config, options);

                using (var stream = new FileStream(_filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                using (var writer = new StreamWriter(stream))
                {
                    await writer.WriteAsync(json);
                    await writer.FlushAsync();
                }
                _logger.LogInfo("Configuración global de la app guardada correctamente.", "AppConfigRepository");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error guardando configuración global en {_filePath}: {ex.Message}", ex, "AppConfigRepository");
                throw;
            }
        }
    }
}
