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
    public class TemplateRepository : ITemplateRepository
    {
        private readonly ILoggingService _logger;
        private readonly string _directoryPath;

        public TemplateRepository(ILoggingService logger)
        {
            _logger = logger;

            string appDataDirectory = OperatingSystem.IsWindows()
                ? Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)
                : Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            _directoryPath = Path.Combine(appDataDirectory, "AppsielPrintManager", "Templates");
            _logger.LogInfo($"[TemplateRepository] Ruta de plantillas (Windows={OperatingSystem.IsWindows()}): {_directoryPath}");

            if (!Directory.Exists(_directoryPath))
            {
                Directory.CreateDirectory(_directoryPath);
            }
        }

        public async Task<PrintTemplate> GetTemplateByTypeAsync(string documentType)
        {
            var fileName = $"{documentType.ToLower()}.json";
            var filePath = Path.Combine(_directoryPath, fileName);

            if (!File.Exists(filePath))
            {
                _logger.LogWarning($"Plantilla para '{documentType}' no encontrada en {filePath}. Generando plantilla por defecto.");

                // Intentar obtener plantilla por defecto
                var defaultTemplate = AppsielPrintManager.Core.Services.DefaultTemplateProvider.GetDefaultTemplate(documentType);

                // Guardar la plantilla por defecto para que el archivo exista físicamente
                await SaveTemplateAsync(defaultTemplate);

                return defaultTemplate;
            }

            try
            {
                var json = await File.ReadAllTextAsync(filePath);
                return JsonSerializer.Deserialize<PrintTemplate>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? throw new InvalidOperationException("Error al deserializar la plantilla.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al leer la plantilla '{documentType}': {ex.Message}", ex);
                return null;
            }
        }

        public async Task SaveTemplateAsync(PrintTemplate template)
        {
            var fileName = $"{(template.DocumentType ?? "unknown").ToLower()}.json";
            var filePath = Path.Combine(_directoryPath, fileName);

            try
            {
                var json = JsonSerializer.Serialize(template, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(filePath, json);
                _logger.LogInfo($"Plantilla '{template.DocumentType}' guardada exitosamente.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al guardar la plantilla '{template.DocumentType}': {ex.Message}", ex);
            }
        }

        public async Task<List<PrintTemplate>> GetAllTemplatesAsync()
        {
            var templates = new List<PrintTemplate>();
            var files = Directory.GetFiles(_directoryPath, "*.json");

            foreach (var file in files)
            {
                try
                {
                    var json = await File.ReadAllTextAsync(file);
                    var template = JsonSerializer.Deserialize<PrintTemplate>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (template != null) templates.Add(template);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error al cargar plantilla desde archivo {file}: {ex.Message}", ex);
                }
            }

            return templates;
        }

        public Task DeleteTemplateAsync(string templateId)
        {
            // En esta implementación, el templateId se correlaciona con el DocumentType para el nombre del archivo
            var filePath = Path.Combine(_directoryPath, $"{templateId.ToLower()}.json");
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.LogInfo($"Plantilla '{templateId}' eliminada.");
            }
            return Task.CompletedTask;
        }

        public async Task EnsureDefaultTemplatesAsync()
        {
            //var requiredTypes = new[] { "ticket_venta", "comanda", "factura_electronica", "sticker_codigo_barras" };
            var requiredTypes = new[] { "comanda", "factura_electronica" };

            foreach (var type in requiredTypes)
            {
                try
                {
                    _logger.LogInfo($"[TemplateRepository] Verificando plantilla: '{type}'");
                    var fileName = $"{type.ToLower()}.json";
                    var filePath = Path.Combine(_directoryPath, fileName);

                    if (!File.Exists(filePath))
                    {
                        _logger.LogInfo($"Validación inicial: Plantilla '{type}' no existe en '{filePath}'. Creándola...");
                        var defaultTemplate = AppsielPrintManager.Core.Services.DefaultTemplateProvider.GetDefaultTemplate(type);
                        if (defaultTemplate == null)
                        {
                            _logger.LogError($"[TemplateRepository] FATAL: GetDefaultTemplate devolvió null para '{type}'");
                            continue;
                        }
                        await SaveTemplateAsync(defaultTemplate);
                        _logger.LogInfo($"[TemplateRepository] Plantilla '{type}' creada exitosamente.");
                    }
                    else
                    {
                        _logger.LogInfo($"[TemplateRepository] Plantilla '{type}' ya existe.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"[TemplateRepository] Error verificando/creando plantilla '{type}': {ex.Message}", ex);
                }
            }
        }

        public async Task<DotMatrixTemplate?> GetDotMatrixTemplateByTypeAsync(string documentType)
        {
            var fileName = $"dm_{documentType.ToLower()}.json";
            var filePath = Path.Combine(_directoryPath, fileName);

            if (!File.Exists(filePath))
            {
                _logger.LogWarning($"Plantilla matricial para '{documentType}' no encontrada en {filePath}.");
                return null;
            }

            try
            {
                var json = await File.ReadAllTextAsync(filePath);
                return JsonSerializer.Deserialize<DotMatrixTemplate>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al leer la plantilla matricial '{documentType}': {ex.Message}", ex);
                return null;
            }
        }

        public async Task SaveDotMatrixTemplateAsync(DotMatrixTemplate template)
        {
            var fileName = $"dm_{(template.DocumentType ?? "unknown").ToLower()}.json";
            var filePath = Path.Combine(_directoryPath, fileName);

            try
            {
                var json = JsonSerializer.Serialize(template, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(filePath, json);
                _logger.LogInfo($"Plantilla matricial '{template.DocumentType}' guardada exitosamente.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al guardar la plantilla matricial '{template.DocumentType}': {ex.Message}", ex);
            }
        }
    }
}
