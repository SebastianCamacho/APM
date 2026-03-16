namespace AppsielPrintManager.Core.Interfaces
{
    /// <summary>
    /// Abstracción para mostrar diálogos/alertas de forma desacoplada de la plataforma.
    /// Permite que los componentes Blazor y servicios no dependan directamente de MAUI.
    /// </summary>
    public interface IDialogService
    {
        /// <summary>
        /// Muestra un diálogo de alerta simple con un botón de confirmación.
        /// </summary>
        Task ShowAlertAsync(string title, string message, string accept);

        /// <summary>
        /// Muestra un diálogo de confirmación con botón de aceptar y cancelar.
        /// Retorna true si el usuario presionó aceptar, false si canceló.
        /// </summary>
        Task<bool> ShowConfirmAsync(string title, string message, string accept, string cancel);
    }
}
