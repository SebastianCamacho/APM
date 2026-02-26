using System;
using Microsoft.Extensions.Logging;
using AppsielPrintManager.Core.Interfaces;

namespace AppsielPrintManager.Infraestructure.Services
{
    /// <summary>
    /// Proveedor de logging que registra el puente AppsielLogger en el sistema de logging de .NET.
    /// </summary>
    public class AppsielLoggerProvider : ILoggerProvider
    {
        private readonly ILoggingService _loggingService;

        public AppsielLoggerProvider(ILoggingService loggingService)
        {
            _loggingService = loggingService;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new AppsielLogger(categoryName, _loggingService);
        }

        public void Dispose()
        {
            // No hay recursos que liberar
        }
    }

    /// <summary>
    /// Implementación de ILogger que redirige los mensajes al ILoggingService personalizado.
    /// </summary>
    public class AppsielLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly ILoggingService _loggingService;

        public AppsielLogger(string categoryName, ILoggingService loggingService)
        {
            _categoryName = categoryName;
            _loggingService = loggingService;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel)
        {
            // Podrías filtrar por nivel aquí si fuera necesario
            return logLevel != Microsoft.Extensions.Logging.LogLevel.None;
        }

        public void Log<TState>(
            Microsoft.Extensions.Logging.LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;

            var message = formatter(state, exception);

            // Simplificar nombre de categoría para que quepa bien en el dashboard
            string service = _categoryName;
            if (service.Contains("."))
            {
                service = service.Substring(service.LastIndexOf(".") + 1);
            }

            switch (logLevel)
            {
                case Microsoft.Extensions.Logging.LogLevel.Trace:
                case Microsoft.Extensions.Logging.LogLevel.Debug:
                    _loggingService.LogDebug(message, service);
                    break;
                case Microsoft.Extensions.Logging.LogLevel.Information:
                    _loggingService.LogInfo(message, service);
                    break;
                case Microsoft.Extensions.Logging.LogLevel.Warning:
                    _loggingService.LogWarning(message, service);
                    break;
                case Microsoft.Extensions.Logging.LogLevel.Error:
                case Microsoft.Extensions.Logging.LogLevel.Critical:
                    _loggingService.LogError(message, exception, service);
                    break;
            }
        }
    }

    /// <summary>
    /// Métodos de extensión para facilitar el registro del puente en la configuración de logging.
    /// </summary>
    public static class AppsielLoggerExtensions
    {
        public static ILoggingBuilder AddAppsielLogger(this ILoggingBuilder builder, ILoggingService loggingService)
        {
            builder.AddProvider(new AppsielLoggerProvider(loggingService));
            return builder;
        }
    }
}
