using AppsielPrintManager.Core.Interfaces;
using System.Threading.Tasks;

namespace AppsielPrintManager.BlazorUI.Services
{
    public class StubPlatformService : IPlatformService
    {
        public bool IsBackgroundServiceRunning => false;

        public bool IsWebSocketServerRunning => false;

        public int CurrentClientCount => 0;

        public Task StartBackgroundServiceAsync()
        {
            // Implementación stub: no hace nada
            return Task.CompletedTask;
        }

        public Task StopBackgroundServiceAsync()
        {
            // Implementación stub: no hace nada
            return Task.CompletedTask;
        }

        public void ShowNotification(string title, string message)
        {
            // Implementación stub: no hace nada
            System.Diagnostics.Debug.WriteLine($"[StubPlatformService] Notificación: {title} - {message}");
        }
    }
}

