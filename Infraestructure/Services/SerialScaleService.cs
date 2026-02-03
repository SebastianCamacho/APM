using AppsielPrintManager.Core.Interfaces;
using AppsielPrintManager.Core.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace AppsielPrintManager.Infraestructure.Services
{
    public class SerialScaleService : IScaleService, IDisposable
    {
        private readonly IScaleRepository _scaleRepository;
        private readonly ILoggingService _logger;

        // ScaleId -> SerialPort
        private readonly ConcurrentDictionary<string, SerialPort> _activePorts = new();

        // ScaleId -> Status
        private readonly ConcurrentDictionary<string, ScaleStatus> _scaleStatuses = new();

        // ScaleId -> Last Error Message
        private readonly ConcurrentDictionary<string, string> _lastErrors = new();

        // ScaleId -> Number of listeners
        private readonly ConcurrentDictionary<string, int> _listenersCount = new();

        // Cache de básculas para evitar consultas constantes a DB
        private List<Scale> _cachedScales = new();
        private readonly SemaphoreSlim _cacheLock = new(1, 1);

        private CancellationTokenSource _cts;
        private Task _monitoringTask;
        private Task _cacheRefreshTask;

        public event EventHandler<ScaleDataEventArgs>? OnWeightChanged;

        public SerialScaleService(IScaleRepository scaleRepository, ILoggingService logger)
        {
            _scaleRepository = scaleRepository;
            _logger = logger;
            _cts = new CancellationTokenSource();
            _monitoringTask = Task.CompletedTask; // Estado inicial válido
            _cacheRefreshTask = Task.CompletedTask;
        }

        public async Task InitializeAsync()
        {
            _logger.LogInfo("Inicializando servicio de básculas seriales...");

            // Cargar inicial
            await RefreshCacheAsync();

            // Iniciar tareas
            _monitoringTask = MonitorConnectionsAsync(_cts.Token);
            _cacheRefreshTask = PeriodicCacheRefreshAsync(_cts.Token);
        }

        public async Task ReloadScalesAsync()
        {
            // Forzar recarga de caché y reinicio de conexiones si es necesario
            _logger.LogInfo("Recargando configuración de básculas...");
            await RefreshCacheAsync();
            // La tarea de monitoreo detectará los cambios en la siguiente iteración
        }

        private async Task RefreshCacheAsync()
        {
            try
            {
                await _cacheLock.WaitAsync();
                var scales = await _scaleRepository.GetAllAsync();
                _cachedScales = scales.Where(s => s.IsActive).ToList();
                _logger.LogInfo($"Caché de básculas actualizado. {_cachedScales.Count} activas.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error actualizando caché de básculas: {ex.Message}");
            }
            finally
            {
                _cacheLock.Release();
            }
        }

        private async Task PeriodicCacheRefreshAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(1), token);
                    await RefreshCacheAsync();
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    _logger.LogError($"Error en ciclo de refresco de caché: {ex.Message}");
                }
            }
        }

        public ScaleStatus GetStatus(string scaleId)
        {
            if (_scaleStatuses.TryGetValue(scaleId, out var status))
            {
                return status;
            }
            return ScaleStatus.Disconnected;
        }

        public List<ScaleStatusInfo> GetAllStatuses()
        {
            return _scaleStatuses.Select(kvp => new ScaleStatusInfo
            {
                ScaleId = kvp.Key,
                Status = kvp.Value,
                ErrorMessage = _lastErrors.TryGetValue(kvp.Key, out var err) ? err : null
            }).ToList();
        }

        public void StartListening(string scaleId)
        {
            _listenersCount.AddOrUpdate(scaleId, 1, (key, count) => count + 1);
            _logger.LogInfo($"Iniciando escucha para báscula {scaleId}. Listeners: {_listenersCount[scaleId]}");
        }

        public void StopListening(string scaleId)
        {
            _listenersCount.AddOrUpdate(scaleId, 0, (key, count) => Math.Max(0, count - 1));
            _logger.LogInfo($"Deteniendo escucha para báscula {scaleId}. Listeners: {_listenersCount[scaleId]}");
        }

        private void TryOpenPort(Scale scale)
        {
            if (_activePorts.ContainsKey(scale.Id)) return;

            try
            {
                // Mapear Enums de Core a System.IO.Ports
                Parity parity = (Parity)(int)scale.Parity;
                StopBits stopBits = (StopBits)(int)scale.StopBits;

                var port = new SerialPort(scale.PortName, scale.BaudRate, parity, scale.DataBits, stopBits);
                port.Handshake = Handshake.None;
                port.DtrEnable = true; // Often required for power or signal
                port.RtsEnable = true; // Often required for power or signal
                port.ReadTimeout = 500;
                port.WriteTimeout = 500;

                // Suscribirse a eventos
                port.DataReceived += (sender, e) => Port_DataReceived(sender, e, scale.Id);

                port.Open();

                if (port.IsOpen)
                {
                    _activePorts[scale.Id] = port;
                    _scaleStatuses[scale.Id] = ScaleStatus.Connected;
                    _lastErrors.TryRemove(scale.Id, out _); // Clear error
                    _logger.LogInfo($"Puerto {scale.PortName} abierto correctamente para báscula {scale.Id}");
                }
            }
            catch (Exception ex)
            {
                _scaleStatuses[scale.Id] = ScaleStatus.Error;
                _lastErrors[scale.Id] = ex.Message; // Store error
                _logger.LogError($"Error al abrir puerto {scale.PortName} para báscula {scale.Id}: {ex.Message}. Config: Baud={scale.BaudRate}, Parity={scale.Parity}, Data={scale.DataBits}, Stop={scale.StopBits}");
            }
        }

        private void Port_DataReceived(object sender, SerialDataReceivedEventArgs e, string scaleId)
        {
            // Solo procesar si hay alguien escuchando
            if (!_listenersCount.TryGetValue(scaleId, out int count) || count <= 0)
            {
                // Limpiar buffer para que no se acumule basura
                try
                {
                    var sp = (SerialPort)sender;
                    if (sp.IsOpen) sp.ReadExisting();
                }
                catch { }
                return;
            }

            try
            {
                SerialPort sp = (SerialPort)sender;
                if (!sp.IsOpen) return;

                // Leer línea
                string line = sp.ReadLine();
                if (string.IsNullOrWhiteSpace(line)) return;

                // Parsear peso (Simple Regex extraction)
                // Buscar números con decimal opcional
                var match = Regex.Match(line, @"(\d+[\.,]?\d*)");
                if (match.Success)
                {
                    string weightStr = match.Groups[1].Value.Replace(',', '.'); // Normalizar decimal
                    if (decimal.TryParse(weightStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal weight))
                    {
                        var data = new ScaleData
                        {
                            ScaleId = scaleId,
                            StationId = "Local", // Ajustar según config
                            Weight = weight,
                            Unit = "kg", // Asumido por ahora
                            Stable = true, // Asumido por ahora
                            Timestamp = DateTime.Now,
                            Type = "SCALE_READING"
                        };

                        OnWeightChanged?.Invoke(this, new ScaleDataEventArgs(scaleId, data));
                    }
                }
            }
            catch (Exception ex)
            {
                // Ignorar timeouts de lectura o errores menores
            }
        }

        private async Task MonitorConnectionsAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    // Usar copia de la caché para iterar
                    List<Scale> currentScales;
                    await _cacheLock.WaitAsync(token);
                    try
                    {
                        currentScales = _cachedScales.ToList();
                    }
                    finally
                    {
                        _cacheLock.Release();
                    }

                    foreach (var scale in currentScales)
                    {
                        try
                        {
                            bool hasPort = _activePorts.TryGetValue(scale.Id, out var port);

                            if (!hasPort)
                            {
                                TryOpenPort(scale);
                            }
                            else
                            {
                                if (port == null || !port.IsOpen)
                                {
                                    _activePorts.TryRemove(scale.Id, out _);
                                    if (port != null) try { port.Dispose(); } catch { }
                                    TryOpenPort(scale);
                                }
                                else
                                {
                                    try
                                    {
                                        var dummy = port.BytesToRead;
                                        _scaleStatuses[scale.Id] = ScaleStatus.Connected;
                                    }
                                    catch (Exception)
                                    {
                                        _logger.LogWarning($"Puerto {scale.PortName} detectado como desconectado (fallo health check).");
                                        try { port.Close(); } catch { }
                                        try { port.Dispose(); } catch { }
                                        _activePorts.TryRemove(scale.Id, out _);
                                        _scaleStatuses[scale.Id] = ScaleStatus.Error;
                                        _lastErrors[scale.Id] = "Dispositivo desconectado.";
                                    }
                                }
                            }
                        }
                        catch (Exception innerEx)
                        {
                            _logger.LogError($"Error procesando báscula {scale.Id}: {innerEx.Message}");
                        }
                    }
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    _logger.LogError($"Error global en monitor de conexiones: {ex.Message}");
                }

                try { await Task.Delay(5000, token); } catch { break; }
            }
        }

        public void Dispose()
        {
            _cts.Cancel();
            foreach (var port in _activePorts.Values)
            {
                if (port.IsOpen) port.Close();
                port.Dispose();
            }
            _activePorts.Clear();
        }

        public ScaleData? GetLastScaleReading()
        {
            return null; // No implementado/No usado en nuevo diseño
        }

        public bool IsConnected => _activePorts.Any(p => p.Value.IsOpen);
    }
}
