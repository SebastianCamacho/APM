using System;
using System.Collections.Generic;

namespace AppsielPrintManager.Core.Interfaces
{
    /// <summary>
    /// Niveles de severidad para los mensajes de log.
    /// </summary>
    public enum LogLevel
    {
        Info,
        Warning,
        Error
    }

    /// <summary>
    /// Representa un mensaje de log con su nivel, contenido y timestamp.
    /// </summary>
    public class LogMessage
    {
        public LogLevel Level { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
        public string FullMessage => $"[{Timestamp:HH:mm:ss}] [{Level.ToString().ToUpper()}] {Message}";
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
        /// Registra un mensaje de información.
        /// </summary>
        void LogInfo(string message);

        /// <summary>
        /// Registra un mensaje de advertencia.
        /// </summary>
        void LogWarning(string message);

        /// <summary>
        /// Registra un mensaje de error con una excepción asociada.
        /// </summary>
        void LogError(string message, Exception exception = null);

        /// <summary>
        /// Obtiene los logs almacenados en disco.
        /// </summary>
        System.Collections.Generic.IReadOnlyList<LogMessage> GetLogs();

        /// <summary>
        /// Borra el archivo de logs.
        /// </summary>
        void ClearLogs();
    }
}
