using AppsielPrintManager.Core.Models;

namespace AppsielPrintManager.Core.Models
{
    /// <summary>
    /// Modelo para recibir una solicitud de actualización de plantilla vía WebSocket.
    /// </summary>
    public class UpdateTemplateRequest
    {
        /// <summary>
        /// Acción a realizar (ej. "UpdateTemplate").
        /// </summary>
        public string Action { get; set; } = string.Empty;

        /// <summary>
        /// La nueva plantilla que se desea guardar.
        /// </summary>
        public PrintTemplate? Template { get; set; }
    }
}
