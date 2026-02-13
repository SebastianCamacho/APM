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

        public bool IsWorkerServiceRunning
        {
            get
            {
                // 1. Si tenemos una referencia local y el proceso sigue vivo, es true.
                if (_workerProcess != null && !_workerProcess.HasExited)
                {
                    return true;
                }

                // 2. Si no, buscamos procesos de forma más amplia (case-insensitive y contains)
                var allProcesses = Process.GetProcesses();
                var serviceProcess = allProcesses.FirstOrDefault(p => p.ProcessName.Contains("WorkerService", StringComparison.OrdinalIgnoreCase));

                if (serviceProcess != null)
                {
                    // Lo encontramos (iniciado externamente o reiniciado), actualizamos referencia.
                    _workerProcess = serviceProcess;
                    return true;
                }

                // 3. No está corriendo ni tenemos referencia válida.
                return false; 
            }
        }

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

                // 1. RUTA DE PRODUCCIÓN (Instalador)
                string productionPath = Path.Combine(appDirectory, "worker", WorkerExeName);

                // 2. RUTAS DE DESARROLLO (Búsqueda robusta y dinámica)
                string solutionRoot = GetDevelopmentSolutionRoot(appDirectory);
                string workerProjectBase = solutionRoot != null ? Path.Combine(solutionRoot, WorkerServiceName) : string.Empty;

                var possiblePaths = new List<string>();

                if (!string.IsNullOrEmpty(workerProjectBase))
                {
                    // Priorizar desarrollo (última compilación en VS, incluyendo RID)
                    possiblePaths.Add(Path.Combine(workerProjectBase, "bin", "Debug", "net10.0", "win-x64", WorkerExeName));
                    possiblePaths.Add(Path.Combine(workerProjectBase, "bin", "Debug", "net10.0", WorkerExeName));
                    possiblePaths.Add(Path.Combine(workerProjectBase, "bin", "Release", "net10.0", "win-x64", WorkerExeName));
                    possiblePaths.Add(Path.Combine(workerProjectBase, "bin", "Release", "net10.0", WorkerExeName));
                    
                    // Versiones con -windows (legacy/fallback)
                    possiblePaths.Add(Path.Combine(workerProjectBase, "bin", "Debug", "net10.0-windows", "win-x64", WorkerExeName));
                    possiblePaths.Add(Path.Combine(workerProjectBase, "bin", "Debug", "net10.0-windows", WorkerExeName));
                    possiblePaths.Add(Path.Combine(workerProjectBase, "bin", "Release", "net10.0-windows", "win-x64", WorkerExeName));
                    possiblePaths.Add(Path.Combine(workerProjectBase, "bin", "Release", "net10.0-windows", WorkerExeName));
                }

                possiblePaths.Add(productionPath);

                workerExePath = possiblePaths.FirstOrDefault(p => File.Exists(p));

                if (!string.IsNullOrEmpty(workerExePath))
                {
                    _logger.LogInfo($"[WorkerServiceManager] Ejecutable encontrado en: '{workerExePath}'");
                }
                else
                {
                    _logger.LogError($"[WorkerServiceManager] No se pudo encontrar el ejecutable '{WorkerExeName}' en ninguna ruta conocida.");
                    return Task.FromResult(false);
                }

                _workerProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = workerExePath,
                        WorkingDirectory = Path.GetDirectoryName(workerExePath), // Asegura que el worker encuentre su hostpolicy.dll y dependencias
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
                
                // Intentar cancelar lectura de streams si fuimos nosotros quienes lo iniciamos.
                // Si el proceso es externo, esto lanzará excepción, así que lo ignoramos.
                try { _workerProcess.CancelOutputRead(); } catch { }
                try { _workerProcess.CancelErrorRead(); } catch { }

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

        private string GetDevelopmentSolutionRoot(string appDirectory)
        {
            var current = new DirectoryInfo(appDirectory);
            while (current != null)
            {
                // Buscamos la carpeta que contiene el proyecto WorkerService o el archivo SLN
                if (Directory.Exists(Path.Combine(current.FullName, WorkerServiceName)) ||
                    File.Exists(Path.Combine(current.FullName, "AppsielPrintManager.slnx")))
                {
                    return current.FullName;
                }
                current = current.Parent;
            }
            return null;
        }
    }
}
#endif