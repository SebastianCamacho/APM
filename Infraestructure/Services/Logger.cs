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
        /// Registra un mensaje de información en la consola.
        /// </summary>
        /// <param name="message">El mensaje informativo a registrar.</param>
        public void LogInfo(string message)
        {
            Console.WriteLine($"[INFO] {DateTime.Now}: {message}");
        }

        /// <summary>
        /// Registra un mensaje de advertencia en la consola.
        /// </summary>
        /// <param name="message">El mensaje de advertencia a registrar.</param>
        public void LogWarning(string message)
        {
            Console.WriteLine($"[WARNING] {DateTime.Now}: {message}");
        }

        /// <summary>
        /// Registra un mensaje de error en la consola, incluyendo detalles de la excepción si se proporciona.
        /// </summary>
        /// <param name="message">El mensaje de error a registrar.</param>
        /// <param name="exception">La excepción opcional asociada al error.</param>
        public void LogError(string message, Exception exception = null)
        {
            Console.Error.WriteLine($"[ERROR] {DateTime.Now}: {message}");
            if (exception != null)
            {
                Console.Error.WriteLine($"Exception: {exception.GetType().Name} - {exception.Message}");
                Console.Error.WriteLine($"StackTrace: {exception.StackTrace}");
            }
        }
    }
}
