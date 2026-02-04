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

                // --- Lógica de Rutas de Producción ---
                

                //TrayApp.exe está en una subcarpeta 'tray' o 'trayapp' dentro del directorio de la UI.exe
                string productionPath = Path.Combine(appDirectory, "trayapp", TrayAppExeName);
                
                


                // --- Lógica de Rutas de Desarrollo ---
                // Si no lo encuentra en la ruta de producción, intentaremos buscarlo en rutas de desarrollo
                string solutionRoot = Path.GetFullPath(Path.Combine(appDirectory, "..", "..", "..", "..", ".."));
                string trayAppProjectBase = Path.Combine(solutionRoot, "TrayApp"); // Ruta corregida al proyecto TrayApp
                string debugPath = Path.Combine(trayAppProjectBase, "bin", "Debug", "net10.0-windows", TrayAppExeName); 
                string releasePath = Path.Combine(trayAppProjectBase, "bin", "Release", "net10.0-windows", TrayAppExeName);


                
                 if (File.Exists(productionPath))
                {
                    trayAppExePath = productionPath;
                    _logger.LogInfo($"[TrayAppService] Usando ruta de producción (subdirectorio 'tray'): '{trayAppExePath}'");
                }
                else if (File.Exists(debugPath))
                {
                    trayAppExePath = debugPath;
                    _logger.LogInfo($"[TrayAppService] Usando ruta de desarrollo (Debug): '{trayAppExePath}'");
                }
                else if (File.Exists(releasePath))
                {
                    trayAppExePath = releasePath;
                    _logger.LogInfo($"[TrayAppService] Usando ruta de desarrollo (Release): '{trayAppExePath}'");
                }
                else
                {
                    _logger.LogError($"[TrayAppService] No se pudo encontrar el ejecutable '{TrayAppExeName}' en ninguna ruta conocida.");
                    return Task.FromResult(false);
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