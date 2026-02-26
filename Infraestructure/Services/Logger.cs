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

        public void LogInfo(string message) => Log(LogLevel.Info, message);
        public void LogWarning(string message) => Log(LogLevel.Warning, message);
        public void LogError(string message, Exception exception = null)
        {
            string full = message;
            if (exception != null) full += $" | Exception: {exception.GetType().Name}: {exception.Message}";
            Log(LogLevel.Error, full);
        }

        private void Log(LogLevel level, string message)
        {
            var log = new LogMessage { Level = level, Message = message, Timestamp = DateTime.Now };

            // 1. Consola/Debug
            Console.WriteLine(log.FullMessage);
            System.Diagnostics.Debug.WriteLine(log.FullMessage);

            // 2. Evento para UI abierta
            OnLogMessage?.Invoke(this, log);

            // 3. Persistencia Asíncrona (Fire & Forget pero seguro)
            _ = WriteToFileAsync(log.FullMessage);
        }

        private async Task WriteToFileAsync(string line)
        {
            if (string.IsNullOrEmpty(_logFilePath)) return;

            await _fileLock.WaitAsync();
            try
            {
                // Rotación si es muy grande
                var fileInfo = new FileInfo(_logFilePath);
                if (fileInfo.Exists && fileInfo.Length > MaxLogSize)
                {
                    try { File.Move(_logFilePath, _logFilePath + ".old", true); } catch { }
                }

                // Intento de escritura con reintentos para colisiones entre procesos (Worker vs UI)
                for (int i = 0; i < 3; i++)
                {
                    try
                    {
                        using var stream = new FileStream(_logFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                        using var writer = new StreamWriter(stream);
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
            finally
            {
                _fileLock.Release();
            }
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
                // Formato: [HH:mm:ss] [LEVEL] Message
                if (line.StartsWith("[") && line.Contains("] ["))
                {
                    int timeEnd = line.IndexOf("]");
                    string timeStr = line.Substring(1, timeEnd - 1);

                    int levelStart = line.IndexOf("[", timeEnd + 1);
                    int levelEnd = line.IndexOf("]", levelStart + 1);
                    string levelStr = line.Substring(levelStart + 1, levelEnd - levelStart - 1);

                    string content = line.Substring(levelEnd + 1).Trim();

                    return new LogMessage
                    {
                        Timestamp = DateTime.ParseExact(timeStr, "HH:mm:ss", null),
                        Level = Enum.Parse<LogLevel>(levelStr, true),
                        Message = content
                    };
                }
            }
            catch { }
            return null;
        }
    }
}
