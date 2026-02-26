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

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private string _selectedLevel = "Todos";

        [ObservableProperty]
        private string _selectedLogCount = "100";

        [ObservableProperty]
        private DateTime _startDate = DateTime.Today.AddDays(-7);

        [ObservableProperty]
        private DateTime _endDate = DateTime.Today.AddDays(1).AddTicks(-1);

        public string[] AvailableLevels { get; } = new[] { "Todos", "Info", "Warning", "Error", "Debug" };
        public string[] AvailableCounts { get; } = new[] { "20", "50", "75", "100", "150", "Todos" };

        public LogsViewModel(ILoggingService loggingService)
        {
            _loggingService = loggingService;
            // Suscribirse al evento de mensajes de log
            _loggingService.OnLogMessage += OnLogMessageReceived;

            LoadHistory();
        }

        partial void OnSearchTextChanged(string value) => LoadHistory();
        partial void OnSelectedLevelChanged(string value) => LoadHistory();
        partial void OnSelectedLogCountChanged(string value) => LoadHistory();
        partial void OnStartDateChanged(DateTime value) => LoadHistory();
        partial void OnEndDateChanged(DateTime value) => LoadHistory();

        [RelayCommand]
        public void RefreshLogs()
        {
            LoadHistory();
        }

        private void LoadHistory()
        {
            Logs.Clear();
            var allLogs = _loggingService.GetLogs()
                .Where(l => l.Timestamp >= StartDate && l.Timestamp <= EndDate);

            if (SelectedLevel != "Todos" && Enum.TryParse<LogLevel>(SelectedLevel, out var levelEnum))
            {
                allLogs = allLogs.Where(l => l.Level == levelEnum);
            }

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var lowerSearch = SearchText.ToLowerInvariant();
                allLogs = allLogs.Where(l =>
                    l.Message.ToLowerInvariant().Contains(lowerSearch) ||
                    l.Service.ToLowerInvariant().Contains(lowerSearch) ||
                    (l.StructuredData != null && l.StructuredData.ToLowerInvariant().Contains(lowerSearch)));
            }

            // Ordenar de más reciente a más antiguo para mejor UX al filtrar/cargar
            var sortedLogs = allLogs.OrderByDescending(l => l.Timestamp).ToList();

            if (SelectedLogCount != "Todos" && int.TryParse(SelectedLogCount, out int limit))
            {
                sortedLogs = sortedLogs.Take(limit).ToList();
            }

            foreach (var log in sortedLogs)
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