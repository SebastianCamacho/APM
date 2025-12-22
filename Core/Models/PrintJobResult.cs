namespace AppsielPrintManager.Core.Models
{
    /// <summary>
    /// Representa el resultado de un trabajo de impresión que se envía de vuelta a Appsiel.
    /// Contiene el ID del trabajo y el estado final (ÉXITO/ERROR).
    /// </summary>
    public class PrintJobResult
    {
        /// <summary>
        /// Identificador único del trabajo de impresión al que se refiere este resultado.
        /// </summary>
        public string JobId { get; set; }

        /// <summary>
        /// Estado final del trabajo de impresión (ej. "DONE", "ERROR").
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Mensaje de error, si el estado es "ERROR", proporcionando detalles sobre lo que falló.
        /// </summary>
        public string ErrorMessage { get; set; }
    }
}
