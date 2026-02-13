namespace AppsielPrintManager.Core.Models
{
    /// <summary>
    /// Representa el resultado de una operación de actualización de plantilla.
    /// </summary>
    public class TemplateUpdateResult
    {
        /// <summary>
        /// Tipo de documento actualizado (ej. "comanda", "ticket_venta").
        /// </summary>
        public string DocumentType { get; set; } = string.Empty;

        /// <summary>
        /// Indica si la operación fue exitosa.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Mensaje descriptivo del resultado (ej. "Actualización completada", "Rechazado por usuario").
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Acción identificadora para el cliente WebSocket. Siempre será "TemplateUpdateResult".
        /// </summary>
        public string Action { get; set; } = "TemplateUpdateResult";
    }
}
