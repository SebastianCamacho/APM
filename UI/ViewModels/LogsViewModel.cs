using CommunityToolkit.Mvvm.ComponentModel;
using AppsielPrintManager.Core.Interfaces;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Maui.Controls; // Para MainThread.BeginInvokeOnMainThread
using CommunityToolkit.Mvvm.Input; // Para [RelayCommand]
using Microsoft.Maui.Dispatching; // Para IDispatcherTimer

namespace UI.ViewModels
{
    public partial class LogsViewModel : ObservableObject
    {
        private readonly ILoggingService _loggingService;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private ObservableCollection<LogMessage> logs = new();

        [ObservableProperty]
        private LogMessage? selectedLog;

        private bool _isAutoRefreshEnabled = true;
        public bool IsAutoRefreshEnabled
        {
            get => _isAutoRefreshEnabled;
            set
            {
                if (SetProperty(ref _isAutoRefreshEnabled, value))
                {
                    OnAutoRefreshChanged(value);
                }
            }
        }

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

        private IDispatcherTimer? _autoRefreshTimer;

        public LogsViewModel(ILoggingService loggingService)
        {
            _loggingService = loggingService;

            // Inicializar Timer
            if (Application.Current?.Dispatcher != null)
            {
                _autoRefreshTimer = Application.Current.Dispatcher.CreateTimer();
                _autoRefreshTimer.Interval = TimeSpan.FromSeconds(2);
                _autoRefreshTimer.Tick += async (s, e) => await LoadHistoryAsync();
            }

            if (_isAutoRefreshEnabled && _autoRefreshTimer != null)
            {
                _autoRefreshTimer.Start();
            }
        }

        private bool _filtersChanged = true;

        partial void OnSearchTextChanged(string value) { _filtersChanged = true; _ = LoadHistoryAsync(); }
        partial void OnSelectedLevelChanged(string value) { _filtersChanged = true; _ = LoadHistoryAsync(); }
        partial void OnSelectedLogCountChanged(string value) { _filtersChanged = true; _ = LoadHistoryAsync(); }
        partial void OnStartDateChanged(DateTime value) { _filtersChanged = true; _ = LoadHistoryAsync(); }
        partial void OnEndDateChanged(DateTime value) { _filtersChanged = true; _ = LoadHistoryAsync(); }

        private void OnAutoRefreshChanged(bool isEnabled)
        {
            if (_autoRefreshTimer == null) return;

            if (isEnabled)
            {
                _autoRefreshTimer.Start();
                _ = LoadHistoryAsync(); // Carga de inmediato al activarlo
            }
            else
            {
                _autoRefreshTimer.Stop();
            }
        }

        [RelayCommand]
        public async Task RefreshLogsAsync()
        {
            await LoadHistoryAsync();
        }

        public async Task LoadHistoryAsync()
        {
            if (IsLoading) return;
            IsLoading = true;

            try
            {
                var sortedLogs = await Task.Run(() =>
                {
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
                    var list = allLogs.OrderByDescending(l => l.Timestamp).ToList();

                    if (SelectedLogCount != "Todos" && int.TryParse(SelectedLogCount, out int limit))
                    {
                        list = list.Take(limit).ToList();
                    }

                    return list;
                });

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (_filtersChanged || Logs.Count == 0)
                    {
                        Logs = new ObservableCollection<LogMessage>(sortedLogs);
                        _filtersChanged = false;
                    }
                    else
                    {
                        var currentTop = Logs.FirstOrDefault();
                        // Realizar inserción incremental solo para nuevos logs
                        if (currentTop != null && sortedLogs.Count > 0 && sortedLogs[0].Timestamp > currentTop.Timestamp)
                        {
                            var newLogs = sortedLogs.TakeWhile(l => l.Timestamp > currentTop.Timestamp).Reverse().ToList();
                            foreach (var nl in newLogs)
                            {
                                Logs.Insert(0, nl);
                            }

                            while (Logs.Count > sortedLogs.Count)
                            {
                                Logs.RemoveAt(Logs.Count - 1);
                            }
                        }
                    }
                });
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        public async Task ClearLogsAsync()
        {
            if (Shell.Current == null) return;

            bool confirm = await Shell.Current.DisplayAlert(
                "¡Advertencia!",
                "Estás a punto de eliminar todos los registros del archivo permanentemente. Esta acción no se puede deshacer.\n\n¿Deseas continuar?",
                "Sí, limpiar todo",
                "Cancelar");

            if (confirm)
            {
                _loggingService.ClearLogs();
                Logs.Clear();
                _filtersChanged = true;
                await LoadHistoryAsync(); // Para reflejar la UI vacía
            }
        }
    }
}