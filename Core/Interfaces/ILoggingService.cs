using System;

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
    /// Esto permite a los componentes de la aplicación registrar mensajes, advertencias y errores
    /// de una manera desacoplada de la implementación específica del logger (ej. consola, archivo, base de datos).
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
        /// <param name="message">El mensaje a registrar.</param>
        void LogInfo(string message);

        /// <summary>
        /// Registra un mensaje de advertencia.
        /// </summary>
        /// <param name="message">El mensaje a registrar.</param>
        void LogWarning(string message);

        /// <summary>
        /// Registra un mensaje de error con una excepción asociada.
        /// </summary>
        /// <param name="message">El mensaje de error a registrar.</param>
        /// <param name="exception">La excepción asociada al error.</param>
        void LogError(string message, System.Exception exception = null);
    }
}
