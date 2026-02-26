using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AppsielPrintManager.Core.Interfaces;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using System;

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

        [ObservableProperty]
        private bool isWebSocketServerRunning;

        [ObservableProperty]
        private int currentClientCount;

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
                // Request Notification Permission for Android 13+
                if (OperatingSystem.IsAndroidVersionAtLeast(33))
                {
                    var status = await Permissions.CheckStatusAsync<Permissions.PostNotifications>();
                    if (status != PermissionStatus.Granted)
                    {
                        status = await Permissions.RequestAsync<Permissions.PostNotifications>();
                        if (status != PermissionStatus.Granted)
                        {
                            _logger.LogWarning("Notification permission denied. Foreground Service notification might not show.", "HomeViewModel");
                            // We can choose to return here, or proceed (service will run but notification suppressed)
                        }
                    }
                }

                if (!_platformService.IsBackgroundServiceRunning)
                {
                    await _platformService.StartBackgroundServiceAsync();
                    _logger.LogInfo("Foreground Service start requested.", "HomeViewModel");
                }
                else
                {
                    _logger.LogInfo("Foreground Service is already running.", "HomeViewModel");
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"Error starting foreground service: {ex.Message}", ex, "HomeViewModel");
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
                    _logger.LogInfo("Foreground Service stop requested.", "HomeViewModel");
                }
                else
                {
                    _logger.LogInfo("Foreground Service is not running.", "HomeViewModel");
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"Error stopping foreground service: {ex.Message}", ex, "HomeViewModel");
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

            // Actualizar estado del WebSocket
            IsWebSocketServerRunning = _platformService.IsWebSocketServerRunning;
            CurrentClientCount = _platformService.CurrentClientCount;

            // _logger.LogInfo($"Service status updated to: {ServiceStatus}, WebSocket: {(IsWebSocketServerRunning ? "Running" : "Stopped")}, Clients: {CurrentClientCount}", "HomeViewModel");
        }

        private bool _isMonitoring;

        public async void StartMonitoring()
        {
            if (_isMonitoring) return;
            _isMonitoring = true;
            _logger.LogInfo("Monitoring started.", "HomeViewModel");
            while (_isMonitoring)
            {
                UpdateServiceStatus();
                // Check status every 1 second
                await Task.Delay(1000);
            }
        }

        public void StopMonitoring()
        {
            _isMonitoring = false;
            _logger.LogInfo("Monitoring stopped.", "HomeViewModel");
        }
    }
}
