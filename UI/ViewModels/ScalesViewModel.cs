using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AppsielPrintManager.Core.Interfaces;
using AppsielPrintManager.Core.Models;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System;
using System.Collections.Generic;
using System.Threading;

namespace UI.ViewModels
{
    public partial class ScalesViewModel : ObservableObject, IDisposable
    {
        private readonly IScaleRepository _scaleRepository;
        private readonly ILoggingService _logger;
        private readonly HttpClient _httpClient;
        private CancellationTokenSource _cts;

        [ObservableProperty]
        public ObservableCollection<ScaleViewModelItem> scales;

        [ObservableProperty]
        public bool isBusy;

        // URL para obtener el estado desde el WorkerService
        private const string StatusUrl = "http://localhost:7000/websocket/status";

        public ScalesViewModel(IScaleRepository scaleRepository, ILoggingService logger)
        {
            _scaleRepository = scaleRepository;
            _logger = logger;
            Scales = new ObservableCollection<ScaleViewModelItem>();
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(1);
            _cts = new CancellationTokenSource();

            // Iniciar polling de estado
            _ = MonitorStatusesAsync(_cts.Token);
        }

        [RelayCommand]
        public async Task LoadScales()
        {
            IsBusy = true;
            try
            {
                await SmoothRefreshScales();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error cargando básculas: {ex.Message}", ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task SmoothRefreshScales()
        {
            var loadedScales = await _scaleRepository.GetAllAsync();
            var sortedScales = loadedScales.OrderBy(s => s.Id).ToList();

            // 1. Eliminar las que ya no existen
            for (int i = Scales.Count - 1; i >= 0; i--)
            {
                if (!sortedScales.Any(s => s.Id == Scales[i].Scale.Id))
                {
                    Scales.RemoveAt(i);
                }
            }

            // 2. Agregar o Actualizar
            foreach (var scale in sortedScales)
            {
                var existingItem = Scales.FirstOrDefault(VM => VM.Scale.Id == scale.Id);
                if (existingItem != null)
                {
                    // ACTUALIZAR PROPIEDADES SI CAMBIARON (Ej: PortName)
                    if (existingItem.Scale.PortName != scale.PortName || existingItem.Scale.Name != scale.Name)
                    {
                        int index = Scales.IndexOf(existingItem);
                        var newItem = new ScaleViewModelItem(scale) { Status = existingItem.Status, ErrorMessage = existingItem.ErrorMessage };
                        Scales[index] = newItem;
                    }
                }
                else
                {
                    Scales.Add(new ScaleViewModelItem(scale));
                }
            }
        }

        [RelayCommand]
        public async Task AddScale()
        {
            await Shell.Current.GoToAsync("ScaleDetailView");
        }

        [RelayCommand]
        public async Task EditScale(ScaleViewModelItem scaleItem)
        {
            if (scaleItem == null) return;
            await Shell.Current.GoToAsync($"ScaleDetailView?scaleId={scaleItem.Scale.Id}");
        }

        [RelayCommand]
        public async Task DeleteScale(ScaleViewModelItem scaleItem)
        {
            if (scaleItem == null) return;
            bool confirm = await Shell.Current.DisplayAlert("Confirmar", $"¿Eliminar báscula '{scaleItem.Scale.Name}'?", "Sí", "No");
            if (confirm)
            {
                try
                {
                    await _scaleRepository.DeleteAsync(scaleItem.Scale.Id);
                    Scales.Remove(scaleItem);

                    // Notificar al WorkerService para que recargue la caché
                    try
                    {
                        await _httpClient.GetAsync("http://localhost:7000/websocket/reload-scales");
                    }
                    catch (Exception reloadEx)
                    {
                        _logger.LogWarning($"No se pudo notificar recarga al WorkerService: {reloadEx.Message}");
                    }
                }
                catch (Exception ex)
                {
                    await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
                }
            }
        }

        private async Task MonitorStatusesAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    // Polling al servicio worker
                    // Esperamos una estructura JSON que coincida con lo enviado por WebSocketServerService
                    // { IsRunning, ConnectedClients, ScaleStatuses }
                    var json = await _httpClient.GetStringAsync(StatusUrl);
                    var statusData = JsonSerializer.Deserialize<WorkerStatusDto>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (statusData != null && statusData.ScaleStatuses != null)
                    {
                        foreach (var statusInfo in statusData.ScaleStatuses)
                        {
                            var item = Scales.FirstOrDefault(s => s.Scale.Id == statusInfo.ScaleId);
                            if (item != null)
                                if (item != null)
                                {
                                    item.Status = statusInfo.Status;
                                    item.ErrorMessage = statusInfo.ErrorMessage;
                                }
                        }
                    }
                }
                catch (Exception)
                {
                    // Si falla la conexión, asumir desconectado para todas (o mantener último estado)
                    // _logger.LogWarning($"No se pudo conectar al servicio de estado: {ex.Message}");
                }

                await Task.Delay(1000, token);

                // RECUPERACIÓN AUTOMÁTICA DE CONFIGURACIÓN
                // Si el backend cambió el puerto (Auto-Detect), la UI necesita enterarse.
                // Recargamos la lista completa cada 3 ciclos (3 segundos aprox) para refrescar "COMx"
                if (DateTime.Now.Second % 3 == 0)
                {
                    // Ejecutar en hilo UI principal porque LoadScales toca ObservableCollection
                    MainThread.BeginInvokeOnMainThread(async () => await LoadScales());
                }
            }
        }

        public void Dispose()
        {
            _cts.Cancel();
            _httpClient.Dispose();
        }
    }

    public partial class ScaleViewModelItem : ObservableObject
    {
        public Scale Scale { get; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(StatusColor))]
        public ScaleStatus status;

        [ObservableProperty]
        public string? errorMessage;

        public string StatusColor => Status switch
        {
            ScaleStatus.Connected => "Green",
            ScaleStatus.Error => "Red",
            _ => "Gray"
        };

        public ScaleViewModelItem(Scale scale)
        {
            Scale = scale;
            Status = ScaleStatus.Disconnected;
        }
    }

    // DTO para deserializar respuesta del worker
    public class WorkerStatusDto
    {
        public bool IsRunning { get; set; }
        public int ConnectedClients { get; set; }
        public List<ScaleStatusInfo> ScaleStatuses { get; set; } = new();
    }
}
