using CommunityToolkit.Mvvm.ComponentModel;
using AppsielPrintManager.Core.Interfaces;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Maui.Controls; // Para MainThread.BeginInvokeOnMainThread

namespace UI.ViewModels
{
    public partial class LogsViewModel : ObservableObject
    {
        private readonly ILoggingService _loggingService;

        [ObservableProperty]
        public ObservableCollection<LogMessage> logs;

        public LogsViewModel(ILoggingService loggingService)
        {
            _loggingService = loggingService;
            Logs = new ObservableCollection<LogMessage>();

            // Suscribirse al evento de mensajes de log (DESACTIVADO TEMPORALMENTE PARA COMPILACIÓN)
            // _loggingService.OnLogMessage += OnLogMessageReceived;
        }

        private void OnLogMessageReceived(object sender, LogMessage message)
        {
            // Asegurarse de que la actualización de la UI ocurra en el hilo principal
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Logs.Add(message);
                // Si la colección crece mucho, se puede implementar una lógica de truncamiento aquí
                // Por ejemplo, mantener solo los últimos N mensajes.
                if (Logs.Count > 100) 
                {
                    Logs.RemoveAt(0);
                }
            });
        }
    }
}