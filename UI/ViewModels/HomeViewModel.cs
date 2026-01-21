using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AppsielPrintManager.Core.Interfaces;
using System.Threading.Tasks;

namespace UI.ViewModels
{
    public partial class HomeViewModel : ObservableObject
    {
        private readonly IPlatformService _platformService;
        private readonly ILoggingService _logger;

        [ObservableProperty]
        private string serviceStatus = "Detenido"; // Initial status for display

        [ObservableProperty]
        private bool isServiceRunning;

        [ObservableProperty]
        private bool isBusy;

        public HomeViewModel(IPlatformService platformService, ILoggingService logger)
        {
            _platformService = platformService;
            _logger = logger;
            UpdateServiceStatus();
        }

        [RelayCommand]
        private async Task StartForegroundService()
        {
            IsBusy = true;
            try
            {
                if (!_platformService.IsBackgroundServiceRunning)
                {
                    await _platformService.StartBackgroundServiceAsync();
                    _logger.LogInfo("HomeViewModel: Foreground Service start requested.");
                }
                else
                {
                    _logger.LogInfo("HomeViewModel: Foreground Service is already running.");
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"HomeViewModel: Error starting foreground service: {ex.Message}", ex);
            }
            finally
            {
                UpdateServiceStatus();
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task StopForegroundService()
        {
            IsBusy = true;
            try
            {
                if (_platformService.IsBackgroundServiceRunning)
                {
                    await _platformService.StopBackgroundServiceAsync();
                    _logger.LogInfo("HomeViewModel: Foreground Service stop requested.");
                }
                else
                {
                    _logger.LogInfo("HomeViewModel: Foreground Service is not running.");
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"HomeViewModel: Error stopping foreground service: {ex.Message}", ex);
            }
            finally
            {
                UpdateServiceStatus();
                IsBusy = false;
            }
        }

        public void UpdateServiceStatus()
        {

            IsServiceRunning = _platformService.IsBackgroundServiceRunning;
            ServiceStatus = IsServiceRunning ? "En ejecución" : "Detenido";
            _logger.LogInfo($"HomeViewModel: Service status updated to: {ServiceStatus}");
        }
    }
}
