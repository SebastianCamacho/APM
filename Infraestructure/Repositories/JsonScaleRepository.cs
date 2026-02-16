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
    public class JsonScaleRepository : IScaleRepository
    {
        private readonly ILoggingService _logger;
        private readonly string _filePath;
        private const string ScalesFileName = "scales.json";

        public JsonScaleRepository(ILoggingService logger)
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

            _filePath = Path.Combine(appSpecificDirectory, ScalesFileName);
            _logger.LogInfo($"[JsonScaleRepository] Ruta de persistencia de básculas (Windows={OperatingSystem.IsWindows()}): {_filePath}");
        }

        public async Task<List<Scale>> GetAllAsync()
        {
            return await LoadAllScalesAsync();
        }

        public async Task<Scale?> GetByIdAsync(string id)
        {
            var scales = await LoadAllScalesAsync();
            return scales.FirstOrDefault(s => s.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
        }

        public async Task AddAsync(Scale scale)
        {
            scale.Id = NormalizeId(scale.Id);
            var scales = await LoadAllScalesAsync();

            if (scales.Any(s => s.Id == scale.Id))
            {
                throw new InvalidOperationException($"Ya existe una báscula con el ID {scale.Id}");
            }

            scales.Add(scale);
            await SaveAllScalesAsync(scales);
            _logger.LogInfo($"Báscula agregada: {scale.Id}");
        }

        public async Task UpdateAsync(Scale scale)
        {
            scale.Id = NormalizeId(scale.Id);
            var scales = await LoadAllScalesAsync();
            var existing = scales.FirstOrDefault(s => s.Id == scale.Id);

            if (existing != null)
            {
                scales.Remove(existing);
                scales.Add(scale);
                await SaveAllScalesAsync(scales);
                _logger.LogInfo($"Báscula actualizada: {scale.Id}");
            }
            else
            {
                _logger.LogWarning($"Intento de actualizar báscula inexistente: {scale.Id}");
            }
        }

        public async Task DeleteAsync(string id)
        {
            id = NormalizeId(id);
            var scales = await LoadAllScalesAsync();
            var removed = scales.RemoveAll(s => s.Id == id);

            if (removed > 0)
            {
                await SaveAllScalesAsync(scales);
                _logger.LogInfo($"Báscula eliminada: {id}");
            }
        }

        private async Task<List<Scale>> LoadAllScalesAsync()
        {
            if (!File.Exists(_filePath))
            {
                return new List<Scale>();
            }

            try
            {
                using var stream = File.Open(_filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(stream);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var json = await reader.ReadToEndAsync();
                return JsonSerializer.Deserialize<List<Scale>>(json, options) ?? new List<Scale>();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error cargando básculas: {ex.Message}", ex);
                return new List<Scale>();
            }
        }

        private async Task SaveAllScalesAsync(List<Scale> scales)
        {
            const int maxRetries = 5;
            const int delayMs = 300;

            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(scales, options);

            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    using (var stream = new FileStream(_filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                    using (var writer = new StreamWriter(stream))
                    {
                        await writer.WriteAsync(json);
                        await writer.FlushAsync();
                    }
                    return;
                }
                catch (IOException ioEx)
                {
                    if (i == maxRetries - 1)
                    {
                        _logger.LogError($"Fallo crítico al guardar básculas tras {maxRetries} intentos: {ioEx.Message}", ioEx);
                        throw;
                    }
                    _logger.LogWarning($"Archivo de básculas bloqueado, reintentando ({i + 1}/{maxRetries})...");
                    await Task.Delay(delayMs);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error inesperado al guardar básculas: {ex.Message}", ex);
                    throw;
                }
            }
        }

        private string NormalizeId(string id)
        {
            return id?.Trim().ToLowerInvariant() ?? string.Empty;
        }
    }
}
