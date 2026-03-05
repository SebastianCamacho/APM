using AppsielPrintManager.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AppsielPrintManager.Infraestructure.Services
{
    /// <summary>
    /// Implementación del servicio de logging que escribe mensajes en la consola y en un archivo persistente.
    /// Soporta concurrencia multi-proceso y rotación de archivos.
    /// </summary>
    public class Logger : ILoggingService
    {
        private readonly string _logFilePath;
        private readonly SemaphoreSlim _fileLock = new(1, 1);
        private const long MaxLogSize = 2 * 1024 * 1024; // 2MB

        public Logger()
        {
            // Ruta compartida entre procesos (ProgramData en Windows para acceso común)
            string appDir;
            if (OperatingSystem.IsWindows())
            {
                appDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "AppsielPrintManager");
            }
            else
            {
                string basePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                appDir = Path.Combine(basePath, "Appsiel", "APM");
            }

            if (!Directory.Exists(appDir)) Directory.CreateDirectory(appDir);

            _logFilePath = Path.Combine(appDir, "apm_activity.log");
        }

        public event EventHandler<LogMessage> OnLogMessage;

        public void LogDebug(string message, string? service = null, object? metadata = null) => Log(LogLevel.Debug, message, service, metadata);
        public void LogInfo(string message, string? service = null, object? metadata = null) => Log(LogLevel.Info, message, service, metadata);
        public void LogWarning(string message, string? service = null, object? metadata = null) => Log(LogLevel.Warning, message, service, metadata);
        public void LogError(string message, Exception? exception = null, string? service = null, object? metadata = null)
        {
            Log(LogLevel.Error, message, service, metadata, exception);
        }

        private void Log(LogLevel level, string message, string? service, object? metadata, Exception? exception = null)
        {
            // Auto-Detección de Servicio si no se provee
            if (string.IsNullOrEmpty(service))
            {
                // Si el mensaje empieza como "HomeViewModel: algo", extraemos HomeViewModel
                if (message.Contains(": "))
                {
                    int idx = message.IndexOf(": ");
                    string potentialService = message.Substring(0, idx).Trim();
                    // Solo si parece un nombre de clase/servicio (sin espacios internos)
                    if (!potentialService.Contains(" "))
                    {
                        service = potentialService;
                        message = message.Substring(idx + 2).Trim();
                    }
                }
            }

            var log = new LogMessage
            {
                Level = level,
                Message = message,
                Timestamp = DateTime.Now,
                Service = service ?? "System"
            };

            // Metadatos estructurados
            var metaDict = new Dictionary<string, object?>();
            if (metadata != null)
            {
                if (metadata is IDictionary<string, object?> dict) metaDict = new Dictionary<string, object?>(dict);
                else metaDict["data"] = metadata;
            }
            if (exception != null)
            {
                metaDict["exception"] = new { exception.GetType().Name, exception.Message, exception.StackTrace };
            }

            if (metaDict.Count > 0)
            {
                try { log.StructuredData = System.Text.Json.JsonSerializer.Serialize(metaDict); } catch { }
            }

            // Fire & Forget de escritura
            Console.WriteLine(log.FullMessage);
            System.Diagnostics.Debug.WriteLine(log.FullMessage);
            OnLogMessage?.Invoke(this, log);
            _ = WriteToFileAsync(log);
        }

        private async Task WriteToFileAsync(LogMessage log)
        {
            if (string.IsNullOrEmpty(_logFilePath)) return;

            await _fileLock.WaitAsync();
            try
            {
                var fileInfo = new FileInfo(_logFilePath);
                if (fileInfo.Exists && fileInfo.Length > MaxLogSize)
                {
                    try { File.Move(_logFilePath, _logFilePath + ".old", true); } catch { }
                }

                for (int i = 0; i < 3; i++)
                {
                    try
                    {
                        using var stream = new FileStream(_logFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                        using var writer = new StreamWriter(stream);

                        string line = log.FullMessage;
                        if (!string.IsNullOrEmpty(log.StructuredData)) line += $" | DATA: {log.StructuredData}";

                        await writer.WriteLineAsync(line);
                        break;
                    }
                    catch (IOException)
                    {
                        if (i == 2) throw;
                        await Task.Delay(50);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Logger Error: No se pudo escribir en disco. {ex.Message}");
            }
            finally { _fileLock.Release(); }
        }

        public IReadOnlyList<LogMessage> GetLogs()
        {
            var list = new List<LogMessage>();
            try
            {
                if (File.Exists(_logFilePath))
                {
                    using var stream = new FileStream(_logFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using var reader = new StreamReader(stream);

                    string? line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        var msg = ParseLogLine(line!);
                        if (msg != null) list.Add(msg);
                    }
                }
            }
            catch { }
            return list.AsReadOnly();
        }

        public void ClearLogs()
        {
            try { if (File.Exists(_logFilePath)) File.Delete(_logFilePath); } catch { }
        }

        private LogMessage? ParseLogLine(string line)
        {
            try
            {
                // Formato: [2026-02-25 18:24:01.123] [LEVEL] [SERVICE] Message | DATA: {...}
                if (line.StartsWith("["))
                {
                    int firstClose = line.IndexOf("]");
                    string dateStr = line.Substring(1, firstClose - 1);

                    int levelOpen = line.IndexOf("[", firstClose + 1);
                    int levelClose = line.IndexOf("]", levelOpen + 1);
                    string levelStr = line.Substring(levelOpen + 1, levelClose - levelOpen - 1).Trim();

                    int serviceOpen = line.IndexOf("[", levelClose + 1);
                    int serviceClose = line.IndexOf("]", serviceOpen + 1);
                    string serviceStr = line.Substring(serviceOpen + 1, serviceClose - serviceOpen - 1).Trim();

                    string remaining = line.Substring(serviceClose + 1).Trim();
                    string messageStr = remaining;
                    string? dataStr = null;

                    if (remaining.Contains(" | DATA: "))
                    {
                        int dataIdx = remaining.IndexOf(" | DATA: ");
                        messageStr = remaining.Substring(0, dataIdx).Trim();
                        dataStr = remaining.Substring(dataIdx + 9).Trim();
                    }

                    return new LogMessage
                    {
                        Timestamp = DateTime.Parse(dateStr),
                        Level = Enum.Parse<LogLevel>(levelStr, true),
                        Service = serviceStr,
                        Message = messageStr,
                        StructuredData = dataStr
                    };
                }
            }
            catch { }
            return null;
        }
    }
}
