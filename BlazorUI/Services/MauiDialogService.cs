using AppsielPrintManager.Core.Interfaces;

namespace BlazorUI.Services
{
    /// <summary>
    /// Implementación MAUI del servicio de diálogos.
    /// Usa Application.Current.Windows (API moderna, sin obsoletos) para obtener la Page activa.
    /// </summary>
    public class MauiDialogService : IDialogService
    {
        /// <summary>
        /// Obtiene la Page activa de la ventana principal de la app.
        /// </summary>
        private static Page? GetActivePage()
            => Application.Current?.Windows.FirstOrDefault()?.Page;

        /// <inheritdoc/>
        public async Task ShowAlertAsync(string title, string message, string accept)
        {
            var page = GetActivePage();
            if (page != null)
            {
                await page.DisplayAlert(title, message, accept);
            }
        }

        /// <inheritdoc/>
        public async Task<bool> ShowConfirmAsync(string title, string message, string accept, string cancel)
        {
            var page = GetActivePage();
            if (page != null)
            {
                return await page.DisplayAlert(title, message, accept, cancel);
            }

            return false;
        }
    }
}
