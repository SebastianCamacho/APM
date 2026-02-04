using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AppsielPrintManager.Core.Interfaces;
using AppsielPrintManager.Core.Models;
using AppsielPrintManager.Core.Enums;
using System.Collections.ObjectModel;
#if WINDOWS
using System.IO.Ports;
#endif
using System.Threading.Tasks;
using System;
using System.Linq;
using Microsoft.Maui.Devices; // For platform check

namespace UI.ViewModels
{
    [QueryProperty(nameof(ScaleId), "scaleId")]
    public partial class ScaleDetailViewModel : ObservableObject
    {
        private readonly IScaleRepository _scaleRepository;
        private readonly ILoggingService _logger;

        [ObservableProperty]
        public string scaleId = string.Empty;

        [ObservableProperty]
        public string name = string.Empty;

        [ObservableProperty]
        public string id = string.Empty;

        [ObservableProperty]
        public string portName = string.Empty;

        [ObservableProperty]
        public int baudRate = 9600;

        [ObservableProperty]
        public int dataBits = 8;

        [ObservableProperty]
        public ScaleParity parity = ScaleParity.None;

        [ObservableProperty]
        public ScaleStopBits stopBits = ScaleStopBits.One;

        [ObservableProperty]
        public ObservableCollection<string> availablePorts = new();

        [ObservableProperty]
        public ObservableCollection<ScaleParity> parityOptions;

        [ObservableProperty]
        public ObservableCollection<ScaleStopBits> stopBitsOptions;

        [ObservableProperty]
        public bool isNew = true;

        [ObservableProperty]
        public bool isBusy;

        public ScaleDetailViewModel(IScaleRepository scaleRepository, ILoggingService logger)
        {
            _scaleRepository = scaleRepository;
            _logger = logger;

            ParityOptions = new ObservableCollection<ScaleParity>(Enum.GetValues(typeof(ScaleParity)).Cast<ScaleParity>());
            StopBitsOptions = new ObservableCollection<ScaleStopBits>(Enum.GetValues(typeof(ScaleStopBits)).Cast<ScaleStopBits>());

            LoadPorts();
        }

        partial void OnScaleIdChanged(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                IsNew = false;
                _ = LoadScaleAsync(value);
            }
        }

        private async Task LoadScaleAsync(string id)
        {
            IsBusy = true;
            try
            {
                var scale = await _scaleRepository.GetByIdAsync(id);
                if (scale != null)
                {
                    Id = scale.Id;
                    Name = scale.Name;
                    PortName = scale.PortName;
                    BaudRate = scale.BaudRate;
                    DataBits = scale.DataBits;
                    Parity = scale.Parity;
                    StopBits = scale.StopBits;
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"Error loading scale: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public void LoadPorts()
        {
            AvailablePorts.Clear();
            try
            {
#if WINDOWS
                if (DeviceInfo.Current.Platform == DevicePlatform.WinUI)
                {
                    var ports = SerialPort.GetPortNames();
                    foreach (var port in ports)
                    {
                        AvailablePorts.Add(port);
                    }
                }
#else
                // Mock ports for non-windows to avoid empty list in UI testing, or just leave empty
                AvailablePorts.Add("COM1");
                AvailablePorts.Add("COM2");
#endif
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error enumerating ports: {ex.Message}");
            }
        }

        [RelayCommand]
        public async Task Save()
        {
            if (string.IsNullOrWhiteSpace(Id) || string.IsNullOrWhiteSpace(Name))
            {
                await Shell.Current.DisplayAlert("Error", "ID and Name are required", "OK");
                return;
            }

            IsBusy = true;
            try
            {
                var scale = new Scale
                {
                    Id = Id,
                    Name = Name,
                    PortName = PortName,
                    BaudRate = BaudRate,
                    DataBits = DataBits,
                    Parity = Parity,
                    StopBits = StopBits,
                    IsActive = true
                };

                if (IsNew)
                {
                    // Check if exists
                    var existing = await _scaleRepository.GetByIdAsync(Id);
                    if (existing != null)
                    {
                        await Shell.Current.DisplayAlert("Error", "ID already exists", "OK");
                        return;
                    }
                    await _scaleRepository.AddAsync(scale);
                }
                else
                {
                    await _scaleRepository.UpdateAsync(scale);
                }

                // Notificar al WorkerService para que recargue la caché
                try
                {
                    using var client = new System.Net.Http.HttpClient();
                    client.Timeout = TimeSpan.FromSeconds(2);
                    await client.GetAsync("http://localhost:7000/websocket/reload-scales");
                }
                catch (Exception reloadEx)
                {
                    _logger.LogWarning($"No se pudo notificar recarga al WorkerService: {reloadEx.Message}");
                }

                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"Error saving: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public async Task Cancel()
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}
