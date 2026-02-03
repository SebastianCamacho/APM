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
        private readonly ConcurrentDictionary<string, int> _listenersCount = new(StringComparer.OrdinalIgnoreCase);

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

        private bool TryOpenPort(Scale scale, string? portNameOverride = null)
        {
            if (_activePorts.ContainsKey(scale.Id)) return true;

            string targetPortName = portNameOverride ?? scale.PortName;

            try
            {
                // Mapear Enums de Core a System.IO.Ports
                Parity parity = (Parity)(int)scale.Parity;
                StopBits stopBits = (StopBits)(int)scale.StopBits;

                var port = new SerialPort(targetPortName, scale.BaudRate, parity, scale.DataBits, stopBits);
                port.Handshake = Handshake.None;

                // Suscribirse a eventos
                port.DataReceived += (sender, e) => Port_DataReceived(sender, e, scale.Id);

                port.Open();

                // IMPORTANTE: Configurar DTR/RTS *después* de abrir el puerto
                port.DtrEnable = true;
                port.RtsEnable = true;
                port.ReadTimeout = 500;
                port.WriteTimeout = 500;

                if (port.IsOpen)
                {
                    _activePorts[scale.Id] = port;
                    _scaleStatuses[scale.Id] = ScaleStatus.Connected;
                    _lastErrors.TryRemove(scale.Id, out _);
                    _logger.LogInfo($"Puerto {targetPortName} abierto correctamente para báscula {scale.Id}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                _scaleStatuses[scale.Id] = ScaleStatus.Error;
                _lastErrors[scale.Id] = ex.Message;

                if (portNameOverride == null || portNameOverride == scale.PortName)
                    _logger.LogError($"Error al abrir puerto {targetPortName} para báscula {scale.Id}: {ex.Message}");
                else
                    _logger.LogInfo($"Intento fallido en {targetPortName} para báscula {scale.Id}: {ex.Message}");
            }
            return false;
        }

        // Este método NO lo llama un bucle "while" ni un "timer". 
        // Es un EVENTHANDLER: Lo llama el Sistema Operativo (Windows) automáticamente 
        // cada vez que llegan bytes eléctricos al puerto USB/Serial.
        // Puede ejecutarse 10 veces por segundo si la báscula es muy rápida.
        private void Port_DataReceived(object sender, SerialDataReceivedEventArgs e, string scaleId)
        {
            try
            {
                // 1. Convertir el objeto generico 'sender' a 'SerialPort' para poder usarlo
                SerialPort sp = (SerialPort)sender;

                // 2. Verificar seguridad: Si el puerto se cerró de golpe (cable desconectado), salir para no chocar.
                if (!sp.IsOpen) return;

                // 3. DIAGNÓSTICO: (Opcional) Ver cuantos bytes hay en espera.
                // int bytesToRead = sp.BytesToRead;

                // --- EXPLICACIÓN DEL CONDICIONAL ANTERIOR ---
                // Este chequeo sirve para AHORRAR CPU.
                // Si nadie (ninguna pantalla) ha mandado "StartListening" (desde el WebSocket), _listenersCount es 0.
                // Si la báscula está enviando 10 datos/seg pero nadie mira la pantalla, los tiramos a la basura
                // para no saturar la CPU procesando Strings y Regex innecesariamente.

                // NOTA IMPORTANTE: Si enviaste "StartListening" y esto sigue siendo 0, es probable que haya
                // un error de Mayúsculas/Minúsculas en el ID de la báscula ("Bacula001" vs "bacula001").
                // He corregido esto haciendo el diccionario 'Insensitive'.
                if (!_listenersCount.TryGetValue(scaleId, out int count) || count <= 0)
                {
                    try { if (sp.IsOpen) sp.ReadExisting(); } catch { } // Limpiar basura
                    return;
                }

                // 4. Leer UNA línea completa de texto que mandó la báscula (hasta el <ENTER> o \n)
                string line = sp.ReadLine();

                // 5. Si la línea está vacía (ruido), ignorarla.
                if (string.IsNullOrWhiteSpace(line)) return;

                // 6. LOG: Ver exactamente qué manda la báscula (ej: "ST,GS,+  15.00kg")
                _logger.LogInfo($"Data recibida ({scaleId}): {line}");

                // 7. REGEX: Usar "Buscador de patrones" para extraer solo el NÚMERO.
                // Patrón: (\d+[\.,]?\d*) significa "Números, tal vez punto o coma, más números".
                var match = Regex.Match(line, @"(\d+[\.,]?\d*)");

                // 8. Si encontramos un número válido en el texto...
                if (match.Success)
                {
                    // 9. Normalizar: Cambiar comas por puntos (C# usa puntos para decimales internamente)
                    string weightStr = match.Groups[1].Value.Replace(',', '.');

                    // 10. Intentar convertir el texto "15.00" a número decimal real
                    if (decimal.TryParse(weightStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal weight))
                    {
                        // 11. Empaquetar los datos en una cajita (Objeto ScaleData)
                        var data = new ScaleData
                        {
                            ScaleId = scaleId,
                            StationId = "Local",
                            Weight = weight,
                            Unit = "kg",
                            Stable = true,
                            Timestamp = DateTime.Now,
                            Type = "SCALE_READING"
                        };

                        // 12. GRITAR EL RESULTADO: "¡Hey! ¡Tengo nuevo peso!"
                        // Esto avisa al WebSocketServerService, que está suscrito a este evento.
                        OnWeightChanged?.Invoke(this, new ScaleDataEventArgs(scaleId, data));
                    }
                }
            }
            catch (Exception ex)
            {
                // Capturar errores (timeouts, ruido) para que el programa no se cierre.
            }
        }

        private async Task MonitorConnectionsAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    // 1. Obtener lista de puertos seguros del SO
                    string[] osPorts = SerialPort.GetPortNames();

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
                                // --- LÓGICA AUTO-DETECT ---
                                // 1. Intentar puerto configurado (Solo si existe en SO)
                                bool found = false;
                                if (osPorts.Any(p => p.Equals(scale.PortName, StringComparison.OrdinalIgnoreCase)))
                                {
                                    if (TryOpenPort(scale, scale.PortName)) found = true;
                                }

                                // 2. Si falló, buscar en otros puertos libres
                                if (!found)
                                {
                                    var usedPorts = _activePorts.Values.Where(p => p.IsOpen).Select(p => p.PortName).ToList();
                                    var candidatePorts = osPorts.Except(usedPorts, StringComparer.OrdinalIgnoreCase);

                                    foreach (var candPort in candidatePorts)
                                    {
                                        if (TryOpenPort(scale, candPort))
                                        {
                                            _logger.LogInfo($"Auto-Detect: Báscula {scale.Id} encontrada en {candPort}");
                                            break;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                // YA CONECTADO -> Health Check
                                if (port == null || !port.IsOpen)
                                {
                                    _activePorts.TryRemove(scale.Id, out _);
                                    if (port != null) try { port.Dispose(); } catch { }
                                    // Reintentar next loop
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
                                        _logger.LogWarning($"Puerto {port.PortName} muerto. Cerrando.");
                                        try { port.Close(); } catch { }
                                        try { port.Dispose(); } catch { }
                                        _activePorts.TryRemove(scale.Id, out _);
                                        _scaleStatuses[scale.Id] = ScaleStatus.Error;
                                        _lastErrors[scale.Id] = "Desconectado.";
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
                    _logger.LogError($"Error global Monitor: {ex.Message}");
                }

                try { await Task.Delay(2000, token); } catch { break; }
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
