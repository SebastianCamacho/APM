using CommunityToolkit.Mvvm.ComponentModel;
using AppsielPrintManager.Core.Interfaces;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Maui.Controls; // Para MainThread.BeginInvokeOnMainThread
using CommunityToolkit.Mvvm.Input; // Para [RelayCommand]

namespace UI.ViewModels
{
    public partial class LogsViewModel : ObservableObject
    {
        private readonly ILoggingService _loggingService;

        [ObservableProperty]
        private ObservableCollection<LogMessage> logs = new();

        [ObservableProperty]
        private LogMessage? selectedLog;

        public LogsViewModel(ILoggingService loggingService)
        {
            _loggingService = loggingService;
            // Suscribirse al evento de mensajes de log
            _loggingService.OnLogMessage += OnLogMessageReceived;

            LoadHistory();
        }

        [RelayCommand]
        public void RefreshLogs()
        {
            LoadHistory();
        }

        private void LoadHistory()
        {
            Logs.Clear();
            foreach (var log in _loggingService.GetLogs())
            {
                Logs.Add(log);
            }
        }

        private void OnLogMessageReceived(object? sender, LogMessage message)
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