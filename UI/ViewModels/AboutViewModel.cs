using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using System;
using System.Threading.Tasks;
using AppsielPrintManager.Core.Interfaces;

namespace UI.ViewModels
{
    public partial class AboutViewModel : ObservableObject
    {
        private readonly IPlatformService _platformService;

        [ObservableProperty]
        private string appVersion;

        [ObservableProperty]
        private bool isServiceRunning;

        [ObservableProperty]
        private string serviceStatusTitle = "Servicio Inactivo";

        public AboutViewModel(IPlatformService platformService)
        {
            _platformService = platformService;
            // Obtener la versión de la aplicación de forma dinámica
            AppVersion = VersionTracking.CurrentVersion;
            UpdateServiceStatus();
        }

        public void UpdateServiceStatus()
        {
            IsServiceRunning = _platformService.IsBackgroundServiceRunning;
            ServiceStatusTitle = IsServiceRunning ? "Servicio Activo" : "Servicio Inactivo";
        }

        private bool _isMonitoring;

        public async void StartMonitoring()
        {
            if (_isMonitoring) return;
            _isMonitoring = true;
            while (_isMonitoring)
            {
                UpdateServiceStatus();
                await Task.Delay(1000);
            }
        }

        public void StopMonitoring()
        {
            _isMonitoring = false;
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
