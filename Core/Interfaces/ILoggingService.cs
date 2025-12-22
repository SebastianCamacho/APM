namespace AppsielPrintManager.Core.Interfaces
{
    /// <summary>
    /// Define la interfaz para un servicio de registro (logging) centralizado.
    /// Esto permite a los componentes de la aplicación registrar mensajes, advertencias y errores
    /// de una manera desacoplada de la implementación específica del logger (ej. consola, archivo, base de datos).
    /// </summary>
    public interface ILoggingService
    {
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
