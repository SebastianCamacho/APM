using System;
using System.Collections.Generic;

namespace AppsielPrintManager.Core.Interfaces
{
    /// <summary>
    /// Niveles de severidad para los mensajes de log.
    /// </summary>
    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error
    }

    /// <summary>
    /// Representa un mensaje de log con su nivel, contenido y metadatos técnicos.
    /// </summary>
    public class LogMessage
    {
        public LogLevel Level { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string Service { get; set; } = "System";
        public string? StructuredData { get; set; }

        public string FullMessage => $"[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level.ToString().ToUpper().PadRight(5)}] [{Service.PadRight(15)}] {Message}";
    }

    /// <summary>
    /// Define la interfaz para un servicio de registro (logging) centralizado.
    /// </summary>
    public interface ILoggingService
    {
        /// <summary>
        /// Evento que se dispara cuando se registra un nuevo mensaje.
        /// </summary>
        event EventHandler<LogMessage> OnLogMessage;

        /// <summary>
        /// Registra un mensaje de depuración.
        /// </summary>
        void LogDebug(string message, string? service = null, object? metadata = null);

        /// <summary>
        /// Registra un mensaje de información.
        /// </summary>
        void LogInfo(string message, string? service = null, object? metadata = null);

        /// <summary>
        /// Registra un mensaje de advertencia.
        /// </summary>
        void LogWarning(string message, string? service = null, object? metadata = null);

        /// <summary>
        /// Registra un mensaje de error con una excepción asociada.
        /// </summary>
        void LogError(string message, Exception? exception = null, string? service = null, object? metadata = null);

        /// <summary>
        /// Obtiene los logs almacenados en disco.
        /// </summary>
        IReadOnlyList<LogMessage> GetLogs();

        /// <summary>
        /// Borra el archivo de logs.
        /// </summary>
        void ClearLogs();
    }
}
