using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using System;
using System.Threading.Tasks;

namespace UI.ViewModels
{
    public partial class AboutViewModel : ObservableObject
    {
        [ObservableProperty]
        private string appVersion;

        public AboutViewModel()
        {
            // Obtener la versión de la aplicación de forma dinámica
            AppVersion = VersionTracking.CurrentVersion;
        }

        [RelayCommand]
        private async Task OpenWebsite()
        {
            try
            {
                Uri uri = new Uri("https://appsiel.com.co/");
                await Browser.Default.OpenAsync(uri, BrowserLaunchMode.SystemPreferred);
            }
            catch (Exception)
            {
                // Un error genérico al abrir el navegador
            }
        }

        [RelayCommand]
        private async Task WhatsAppSupport()
        {
            try
            {
                Uri uri = new Uri("https://api.whatsapp.com/send/?phone=%2B573014159571&text&type=phone_number&app_absent=0");
                await Browser.Default.OpenAsync(uri, BrowserLaunchMode.SystemPreferred);
            }
            catch (Exception)
            {
                // Error al abrir enlace de WhatsApp
            }
        }
    }
}
