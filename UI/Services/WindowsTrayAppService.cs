//#if WINDOWS
using AppsielPrintManager.Core.Interfaces;
using AppsielPrintManager.Infraestructure.Services; // Para ILoggingService
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace UI.Services
{
    public class WindowsTrayAppService : ITrayAppService
    {
        private readonly ILoggingService _logger;
        private Process _trayAppProcess;
        private const string TrayAppProcessName = "TrayApp"; // Nombre del proceso sin la extensión .exe
        private const string TrayAppExeName = "TrayApp.exe"; // Nombre del ejecutable

        public WindowsTrayAppService(ILoggingService logger)
        {
            _logger = logger;

            // Intentar reengancharse si el proceso ya existe
            var existingProcesses = Process.GetProcessesByName(TrayAppProcessName);
            if (existingProcesses.Any())
            {
                _trayAppProcess = existingProcesses.First();
                _logger.LogInfo($"[TrayAppService] TrayApp '{TrayAppProcessName}' ya en ejecución con PID: {_trayAppProcess.Id}");
            }
            else
            {
                _logger.LogInfo($"[TrayAppService] TrayApp '{TrayAppProcessName}' no encontrado en procesos existentes.");
            }
        }

        public bool IsTrayAppRunning => _trayAppProcess != null && !_trayAppProcess.HasExited;

        public Task<bool> StartTrayAppAsync()
        {
            if (IsTrayAppRunning)
            {
                _logger.LogInfo($"[TrayAppService] TrayApp '{TrayAppProcessName}' ya está en ejecución.");
                return Task.FromResult(true);
            }

            try
            {
                string appDirectory = AppContext.BaseDirectory;
                string trayAppExePath = string.Empty;

                // Construir la ruta al ejecutable de TrayApp
                // Asumiendo que TrayApp.exe está en el mismo directorio que UI.exe en el despliegue
                // o en una ruta relativa predecible en desarrollo.
                // Basado en la inspección previa, el ejecutable está en el mismo nivel de 'net10.0-windows'
                string productionPath = Path.Combine(appDirectory, TrayAppExeName);

                // Para desarrollo, podríamos necesitar buscar en las carpetas bin/Debug/net10.0-windows
                // Sin embargo, para la mayoría de los escenarios de publicación y ejecución de la UI,
                // el TrayApp.exe debería estar en un directorio accesible relativo a la UI.exe.
                // Para el propósito de esta tarea, la ruta de producción es la más relevante,
                // ya que la aplicación MAUI se ejecutará desde su directorio de publicación.

                if (File.Exists(productionPath))
                {
                    trayAppExePath = productionPath;
                    _logger.LogInfo($"[TrayAppService] Usando ruta de producción: '{trayAppExePath}'");
                }
                else
                {
                     // Si no lo encuentra en la ruta de producción, intentaremos buscarlo en una ruta de desarrollo
                    // Similar a cómo lo hace WindowsWorkerServiceManager
                    string solutionRoot = Path.GetFullPath(Path.Combine(appDirectory, "..", "..", "..", "..", ".."));
                    string trayAppProjectBase = Path.Combine(solutionRoot, "TrayApp"); // Ajustar la ruta del proyecto TrayApp
                    string debugPath = Path.Combine(trayAppProjectBase, "bin", "Debug", "net10.0-windows", TrayAppExeName);
                    string releasePath = Path.Combine(trayAppProjectBase, "bin", "Release", "net10.0-windows", TrayAppExeName);

                    if (File.Exists(debugPath))
                    {
                        trayAppExePath = debugPath;
                        _logger.LogInfo($"[TrayAppService] Usando ruta Debug: '{trayAppExePath}'");
                    }
                    else if (File.Exists(releasePath))
                    {
                        trayAppExePath = releasePath;
                        _logger.LogInfo($"[TrayAppService] Usando ruta Release: '{trayAppExePath}'");
                    }
                    else
                    {
                        _logger.LogError($"[TrayAppService] No se pudo encontrar el ejecutable '{TrayAppExeName}' en ninguna ruta conocida.");
                        return Task.FromResult(false);
                    }
                }


                _trayAppProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = trayAppExePath,
                        UseShellExecute = true, // Usar true para que se inicie de forma normal
                        CreateNoWindow = false, // Asegurarse de que la ventana se muestre si es una aplicación de UI
                        WorkingDirectory = Path.GetDirectoryName(trayAppExePath) // Establecer el directorio de trabajo
                    }
                };

                _trayAppProcess.Start();

                _logger.LogInfo($"[TrayAppService] TrayApp iniciado con PID: {_trayAppProcess.Id}");
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError($"[TrayAppService] Error al iniciar: {ex.Message}", ex);
                return Task.FromResult(false);
            }
        }
    }
}
//#endif