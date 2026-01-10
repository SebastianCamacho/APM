using AppsielPrintManager.Core.Interfaces;
using System;

namespace AppsielPrintManager.Infraestructure.Services
{
    /// <summary>
    /// Implementación básica del servicio de logging que escribe mensajes en la consola.
    /// Puede ser extendida para escribir en archivos, UI o servicios de telemetría.
    /// </summary>
    public class Logger : ILoggingService
    {
        /// <summary>
        /// Evento que se dispara cuando se registra un nuevo mensaje.
        /// </summary>
        public event EventHandler<LogMessage> OnLogMessage;

        /// <summary>
        /// Registra un mensaje de información en la consola.
        /// </summary>
        /// <param name="message">El mensaje informativo a registrar.</param>
        public void LogInfo(string message)
        {
            var logMessage = new LogMessage { Level = LogLevel.Info, Message = message, Timestamp = DateTime.Now };
            Console.WriteLine(logMessage.FullMessage);
            OnLogMessage?.Invoke(this, logMessage);
        }

        /// <summary>
        /// Registra un mensaje de advertencia en la consola.
        /// </summary>
        /// <param name="message">El mensaje de advertencia a registrar.</param>
        public void LogWarning(string message)
        {
            var logMessage = new LogMessage { Level = LogLevel.Warning, Message = message, Timestamp = DateTime.Now };
            Console.WriteLine(logMessage.FullMessage);
            OnLogMessage?.Invoke(this, logMessage);
        }

        /// <summary>
        /// Registra un mensaje de error en la consola, incluyendo detalles de la excepción si se proporciona.
        /// </summary>
        /// <param name="message">El mensaje de error a registrar.</param>
        /// <param name="exception">La excepción opcional asociada al error.</param>
        public void LogError(string message, Exception exception = null)
        {
            string fullMessage = message;
            if (exception != null)
            {
                fullMessage += $" Exception: {exception.GetType().Name} - {exception.Message} StackTrace: {exception.StackTrace}";
            }
            var logMessage = new LogMessage { Level = LogLevel.Error, Message = fullMessage, Timestamp = DateTime.Now };
            Console.Error.WriteLine(logMessage.FullMessage);
            OnLogMessage?.Invoke(this, logMessage);
        }
    }
}
