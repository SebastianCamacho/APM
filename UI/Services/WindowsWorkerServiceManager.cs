#if WINDOWS
using AppsielPrintManager.Core.Interfaces;
using AppsielPrintManager.Infraestructure.Services;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.ServiceProcess;

namespace UI.Services
{
    public class WindowsWorkerServiceManager : IWorkerServiceManager
    {
        private readonly ILoggingService _logger;
        private Process _workerProcess;
        private const string WorkerServiceName = "WorkerService"; // Nombre del proceso/servicio
        private const string WorkerExeName = "WorkerService.exe"; // Nombre del ejecutable

        public WindowsWorkerServiceManager(ILoggingService logger)
        {
            _logger = logger;
            
            // Intentar reengancharse si el proceso ya existe
            var existingProcesses = Process.GetProcessesByName(WorkerServiceName);
            if (existingProcesses.Any())
            {
                _workerProcess = existingProcesses.First();
                _logger.LogInfo($"[WorkerServiceManager] WorkerService '{WorkerServiceName}' ya en ejecución con PID: {_workerProcess.Id}");
            }
            else
            {
                _logger.LogInfo($"[WorkerServiceManager] WorkerService '{WorkerServiceName}' no encontrado en procesos existentes.");
            }
        }

        public bool IsWorkerServiceRunning => _workerProcess != null && !_workerProcess.HasExited;

        public Task<bool> StartWorkerServiceAsync()
        {
            if (IsWorkerServiceRunning)
            {
                _logger.LogInfo($"[WorkerServiceManager] WorkerService '{WorkerServiceName}' ya está en ejecución.");
                return Task.FromResult(true);
            }

            try
            {
                string appDirectory = AppContext.BaseDirectory;
                string workerExePath = string.Empty;

                // 1. RUTA DE PRODUCCIÓN (Instalador): El worker suele estar en una subcarpeta /worker
                string productionPath = Path.Combine(appDirectory, "worker", WorkerExeName);

                // 2. RUTAS DE DESARROLLO (Tu lógica original)
                string solutionRoot = Path.GetFullPath(Path.Combine(appDirectory, "..", "..", "..", "..", ".."));
                string workerProjectBase = Path.Combine(solutionRoot, WorkerServiceName);
                string debugPath = Path.Combine(workerProjectBase, "bin", "Debug", "net10.0", WorkerExeName);
                string releasePath = Path.Combine(workerProjectBase, "bin", "Release", "net10.0", WorkerExeName);

                if (File.Exists(productionPath))
                {
                    workerExePath = productionPath;
                    _logger.LogInfo($"[WorkerServiceManager] Usando ruta de producción: '{workerExePath}'");
                }
                else if (File.Exists(debugPath))
                {
                    workerExePath = debugPath;
                    _logger.LogInfo($"[WorkerServiceManager] Usando ruta Debug: '{workerExePath}'");
                }
                else if (File.Exists(releasePath))
                {
                    workerExePath = releasePath;
                    _logger.LogInfo($"[WorkerServiceManager] Usando ruta Release: '{workerExePath}'");
                }
                else
                {
                    _logger.LogError($"[WorkerServiceManager] No se pudo encontrar el ejecutable en ninguna ruta.");
                    return Task.FromResult(false);
                }

                _workerProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = workerExePath,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                _workerProcess.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data)) _logger.LogInfo($"[WorkerService OUT] {e.Data}");
                };
                _workerProcess.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data)) _logger.LogError($"[WorkerService ERR] {e.Data}");
                };

                _workerProcess.Start();
                _workerProcess.BeginOutputReadLine();
                _workerProcess.BeginErrorReadLine();
                
                _logger.LogInfo($"[WorkerServiceManager] WorkerService iniciado con PID: {_workerProcess.Id}");
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError($"[WorkerServiceManager] Error al iniciar: {ex.Message}", ex);
                return Task.FromResult(false);
            }
        }

        public async Task<bool> StopWorkerServiceAsync()
        {
            if (!IsWorkerServiceRunning) return true;

            try
            {
                _logger.LogInfo($"[WorkerServiceManager] Deteniendo PID: {_workerProcess.Id}");
                _workerProcess.CancelOutputRead();
                _workerProcess.CancelErrorRead();
                _workerProcess.Kill();
                await _workerProcess.WaitForExitAsync();
                _workerProcess = null;
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[WorkerServiceManager] Error al detener: {ex.Message}", ex);
                return false;
            }
        }
    }
}
#endif