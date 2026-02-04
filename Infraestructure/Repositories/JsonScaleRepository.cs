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
            string appDataDirectory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appSpecificDirectory = Path.Combine(appDataDirectory, "AppsielPrintManager");

            if (!Directory.Exists(appSpecificDirectory))
            {
                Directory.CreateDirectory(appSpecificDirectory);
            }

            _filePath = Path.Combine(appSpecificDirectory, ScalesFileName);
            _logger.LogInfo($"Ruta del archivo de básculas: {_filePath}");
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
                var json = await reader.ReadToEndAsync();
                return JsonSerializer.Deserialize<List<Scale>>(json) ?? new List<Scale>();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error cargando básculas: {ex.Message}", ex);
                return new List<Scale>();
            }
        }

        private async Task SaveAllScalesAsync(List<Scale> scales)
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(scales, options);
                await File.WriteAllTextAsync(_filePath, json);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error guardando básculas: {ex.Message}", ex);
            }
        }

        private string NormalizeId(string id)
        {
            return id?.Trim().ToLowerInvariant() ?? string.Empty;
        }
    }
}
