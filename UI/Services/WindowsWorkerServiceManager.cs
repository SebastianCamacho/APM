#if WINDOWS
using AppsielPrintManager.Core.Interfaces;
using AppsielPrintManager.Infraestructure.Services; // Assuming Logger is in Infraestructure.Services
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace UI.Services
{
    public class WindowsWorkerServiceManager : IWorkerServiceManager
    {
        private readonly ILoggingService _logger;
        private Process _workerProcess;
        private const string WorkerServiceName = "WorkerService"; // Nombre del proyecto del Worker Service
        private const string WorkerExeName = "WorkerService.exe"; // Nombre del ejecutable del Worker Service

        public WindowsWorkerServiceManager(ILoggingService logger)
        {
            _logger = logger;
            // Intentar encontrar un proceso existente del WorkerService al iniciar la UI.
            // Esto es crucial para reengancharse a un servicio ya en ejecución si la UI se reinicia.
            var existingProcesses = Process.GetProcessesByName(WorkerServiceName);
            if (existingProcesses.Any())
            {
                _workerProcess = existingProcesses.First(); // Tomar el primero si hay múltiples (debería ser único)
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
                string appDirectory = AppContext.BaseDirectory; // UI's base directory
                string workerExePath = string.Empty;

                // Navigate up from the UI's bin/Debug/net10.0-windows10.0.19041.0/win-x64/
                // Then navigate down to WorkerService/bin/Debug/net10.0/
                // This is a common pattern when UI and WorkerService are sibling projects in a solution.
                string solutionRoot = Path.GetFullPath(Path.Combine(appDirectory, "..", "..", "..", "..", "..")); // Ajusta basado en tu profundidad real
                _logger.LogInfo($"[WorkerServiceManager] Directorio raíz de la solución inferido: '{solutionRoot}'");

                // Assuming WorkerService is at [SolutionRoot]/WorkerService/
                string workerProjectBase = Path.Combine(solutionRoot, WorkerServiceName);

                // Try Debug path first
                string debugPath = Path.Combine(workerProjectBase, "bin", "Debug", "net10.0", WorkerExeName);
                // Then Release path
                string releasePath = Path.Combine(workerProjectBase, "bin", "Release", "net10.0", WorkerExeName);

                _logger.LogInfo($"[WorkerServiceManager] Intentando ruta Debug: '{debugPath}'");
                _logger.LogInfo($"[WorkerServiceManager] Intentando ruta Release: '{releasePath}'");

                if (File.Exists(debugPath))
                {
                    workerExePath = debugPath;
                }
                else if (File.Exists(releasePath))
                {
                    workerExePath = releasePath;
                }
                else
                {
                    _logger.LogError($"[WorkerServiceManager] No se pudo encontrar el ejecutable del WorkerService en ninguna de las rutas esperadas.");
                    return Task.FromResult(false);
                }
                
                _logger.LogInfo($"[WorkerServiceManager] Ruta final del ejecutable del WorkerService: '{workerExePath}'");

                _workerProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = workerExePath,
                        UseShellExecute = false, // Necesario para no abrir ventana de consola si el worker es un WinUI app.
                        RedirectStandardOutput = true, // Redirigir la salida para que no aparezca en la consola de la UI.
                        RedirectStandardError = true,
                        CreateNoWindow = true // No crear una ventana para el proceso.
                    }
                };

                _workerProcess.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        _logger.LogInfo($"[WorkerService OUT] {e.Data}");
                    }
                };
                _workerProcess.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        _logger.LogError($"[WorkerService ERR] {e.Data}");
                    }
                };

                _workerProcess.Start();
                _workerProcess.BeginOutputReadLine();
                _workerProcess.BeginErrorReadLine();
                _logger.LogInfo($"[WorkerServiceManager] WorkerService '{WorkerServiceName}' iniciado con PID: {_workerProcess.Id}");
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError($"[WorkerServiceManager] Error al iniciar WorkerService: {ex.Message}", ex);
                return Task.FromResult(false);
            }
        }

        public async Task<bool> StopWorkerServiceAsync()
        {
            if (!IsWorkerServiceRunning)
            {
                _logger.LogInfo($"[WorkerServiceManager] WorkerService '{WorkerServiceName}' no está en ejecución.");
                return true;
            }

            try
            {
                _logger.LogInfo($"[WorkerServiceManager] Intentando detener WorkerService con PID: {_workerProcess.Id}");
                // Cerrar las redirecciones de salida antes de terminar el proceso
                _workerProcess.CancelOutputRead();
                _workerProcess.CancelErrorRead();
                
                _workerProcess.Kill(); // Forzar la detención del proceso.
                await _workerProcess.WaitForExitAsync();
                _logger.LogInfo($"[WorkerServiceManager] WorkerService '{WorkerServiceName}' detenido.");
                _workerProcess = null;
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[WorkerServiceManager] Error al detener WorkerService: {ex.Message}", ex);
                return false;
            }
        }
    }
}
#endif
