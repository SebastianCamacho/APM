using System.Diagnostics; // Para Process.Start si se usa para lanzar la UI.
using System.Windows;       // Para System.Windows.MessageBox y System.Windows.Application.
using System.Windows.Forms; // Para NotifyIcon y ContextMenuStrip (elementos de Windows Forms).
using System.Drawing;       // Para System.Drawing.Icon (gestión de iconos).
using System.ServiceProcess; // Para ServiceController (interacción con servicios de Windows).
using System.Timers;        // Para System.Timers.Timer (actualizaciones periódicas).
using Application = System.Windows.Application; // Alias para resolver ambigüedad entre System.Windows.Application y System.Windows.Forms.Application.
using System.IO;            // Para Path.Combine y File.Exists
using System.Linq;          // Para Process.GetProcessesByName

namespace TrayApp.Services;

/// <summary>
/// Gestiona el icono de la aplicación en la bandeja del sistema (System Tray)
/// y proporciona un menú contextual para interactuar con el Appsiel Print Manager.
/// </summary>
public class TrayIcon : IDisposable
{
    // El icono que se muestra en la bandeja del sistema.
    private readonly NotifyIcon _icon;
    // Controlador para interactuar con el WorkerService de Appsiel Print Manager.
    private readonly ServiceController _windowsServiceController; // Renombrado para claridad
    // Temporizador para actualizar periódicamente el estado del WorkerService en el menú.
    private readonly System.Timers.Timer _statusUpdateTimer;

    // Recursos de iconos para el menú contextual.
    private Image _iconStatusRunning;
    private Image _iconStatusStopped; 
    private Image _iconStatusOpen;
    private Image _iconActionStart;
    private Image _iconActionStop;

    // Nombre del servicio de Windows del WorkerService (el nombre registrado en Windows Services).
    private const string WindowsServiceName = "WorkerService"; // Asumiendo que este es el nombre real del servicio
    // Nombre del ejecutable del WorkerService (el archivo .exe que se lanza).
    private const string WorkerExeName = "WorkerService"; // Sin .exe para GetProcessesByName, con .exe para File.Exists y Process.Start
    // Nombre del ejecutable de la interfaz de usuario MAUI.
    private const string MauiUIExeName = "UI"; // Asumiendo que el proyecto MAUI UI se llama "UI" y su ejecutable es UI.exe


    /// <summary>
    /// Constructor de la clase TrayIcon.
    /// Inicializa el icono de la bandeja, el controlador del servicio y el menú contextual.
    /// </summary>
    public TrayIcon()
    {
        // Inicializa el controlador del servicio de Windows.
        _windowsServiceController = new ServiceController(WindowsServiceName);

        // Configura el NotifyIcon.
        _icon = new NotifyIcon
        {
            // Carga el icono desde los recursos del proyecto.
            Icon = new Icon("Resources/apmtrayicon.ico"),
            // Hace visible el icono en la bandeja del sistema.
            Visible = true,
            // Texto que se muestra al pasar el ratón por encima del icono.
            Text = "Appsiel Print Manager"
        };

        // Carga los iconos para los estados y acciones.
        // Se asume que estos archivos PNG están en la carpeta 'Resources' del proyecto.
        _iconStatusRunning = Image.FromFile("Resources/icono_en_ejecucion.png");
        _iconStatusStopped = Image.FromFile("Resources/icono_detenido.png");
        _iconStatusOpen = Image.FromFile("Resources/icono_abrir_ui.png");
        _iconActionStart = Image.FromFile("Resources/icono_iniciar.png");
        _iconActionStop = Image.FromFile("Resources/icono_detener.png");


        // Crea el menú contextual que aparece al hacer clic derecho en el icono.
        var menu = new ContextMenuStrip();
        menu.Items.Add("Abrir UI", _iconStatusOpen, (_, _) => OpenUI()); // Opción para abrir la interfaz de usuario con icono.
        menu.Items.Add("-"); // Separador visual en el menú.
        
        // Elemento de menú que mostrará el estado actual del WorkerService.
        // Se le asigna un nombre ("ServiceStatusMenuItem") para poder referenciarlo y actualizarlo.
        ToolStripMenuItem statusMenuItem = new ToolStripMenuItem("Estado: Desconocido") { Name = "ServiceStatusMenuItem", Enabled = true };
        menu.Items.Add(statusMenuItem);
        
        menu.Items.Add("-"); // Separador visual.
        
        // Opción para iniciar el WorkerService.
        ToolStripMenuItem startServiceMenuItem = new ToolStripMenuItem("Iniciar WorkerService", _iconActionStart, (_, _) => StartWorkerService()) { Name = "StartServiceMenuItem" };
        menu.Items.Add(startServiceMenuItem);
        
        // Opción para detener el WorkerService.
        ToolStripMenuItem stopServiceMenuItem = new ToolStripMenuItem("Detener WorkerService", _iconActionStop, (_, _) => StopWorkerService()) { Name = "StopServiceMenuItem" };
        menu.Items.Add(stopServiceMenuItem);
        
        menu.Items.Add("-"); // Separador visual.
        menu.Items.Add("Salir", _iconActionStop, (_, _) => Exit()); // Opción para salir de la aplicación con icono.

        // Asigna el menú contextual al NotifyIcon.
        _icon.ContextMenuStrip = menu;

        // Configura el temporizador para actualizar el estado del servicio cada 5 segundos.
        _statusUpdateTimer = new System.Timers.Timer(5000);
        // Asocia el método UpdateServiceStatus al evento Elapsed del temporizador.
        _statusUpdateTimer.Elapsed += (sender, e) => UpdateServiceStatus();
        // Inicia el temporizador.
        _statusUpdateTimer.Start();

        // Realiza una actualización inicial del estado del servicio al iniciar.
        UpdateServiceStatus();
    }
    
    /// <summary>
    /// Intenta encontrar la ruta del ejecutable del WorkerService.
    /// Reutiliza la lógica de descubrimiento de rutas del proyecto UI.
    /// </summary>
    /// <returns>La ruta completa al ejecutable del WorkerService o null si no se encuentra.</returns>
    private string FindWorkerServiceExecutablePath()
    {
        string appDirectory = AppContext.BaseDirectory;
        string workerExeFullName = WorkerExeName + ".exe"; // Nombre completo del ejecutable

        // --- Lógica de Rutas de Producción ---
        // Escenario: WorkerService.exe está en una subcarpeta 'worker' que es hermana de 'trayapp'
        // Desde [Carpeta de instalación de la UI]/trayapp/, subimos un nivel a [Carpeta de instalación de la UI]/
        string installRoot = Path.GetFullPath(Path.Combine(appDirectory, ".."));
        // Luego bajamos a 'worker'
        string productionPath = Path.Combine(installRoot, "worker", workerExeFullName);

        // --- Lógica de Rutas de Desarrollo ---
        // Desde appDirectory: E:\...\AppsielPrintManager\TrayApp\bin\Debug\net10.0-windows\
        // Subir 6 niveles para llegar a la carpeta de la solución: E:\Escritorio\PROYECTOS APPSIEL\APM - Appsiel Print Manager\
        string solutionRoot = Path.GetFullPath(Path.Combine(appDirectory, "..", "..", "..", "..", "..")); // 6 ".."
        string workerProjectBase = Path.Combine(solutionRoot, "AppsielPrintManager", "WorkerService"); // Ruta al proyecto WorkerService
        string debugPath = Path.Combine(workerProjectBase, "bin", "Debug", "net10.0", workerExeFullName);
        if (File.Exists(productionPath))
        {
            return productionPath;
        }
        else if (File.Exists(debugPath))
        {
            return debugPath;
        }
        
        return null; // Ejecutable no encontrado en ninguna de las rutas esperadas.
    }

    /// <summary>
    /// Intenta encontrar la ruta del ejecutable de la interfaz de usuario MAUI.
    /// </summary>
    /// <returns>La ruta completa al ejecutable de la UI MAUI o null si no se encuentra.</returns>
    private string FindMauiUIExecutablePath()
    {
        string appDirectory = AppContext.BaseDirectory;
        string uiExeFullName = MauiUIExeName + ".exe";

        // --- Lógica de Rutas de Producción ---
        // Escenario: UI.exe está un nivel arriba del directorio de TrayApp.exe
        // Desde [Carpeta de instalación de la UI]/trayapp/, subimos un nivel a [Carpeta de instalación de la UI]/
        // y allí está UI.exe
        string productionPath = Path.GetFullPath(Path.Combine(appDirectory, "..", uiExeFullName));

        // --- Lógica de Rutas de Desarrollo ---
        // Desde appDirectory: E:\...\AppsielPrintManager\TrayApp\bin\Debug\net10.0-windows\
        // Subir 5 niveles para llegar a la raíz del proyecto AppsielPrintManager (que contiene los proyectos UI, WorkerService, etc.)
        string solutionRoot = Path.GetFullPath(Path.Combine(appDirectory, "..", "..", "..", "..")); // 5 ".."
        string uiProjectBase = Path.Combine(solutionRoot, "UI"); // Ruta al proyecto UI
        string debugPath = Path.Combine(uiProjectBase, "bin", "Debug", "net10.0-windows10.0.19041.0", "win-x64", uiExeFullName); // MAUI es net10.0-windows
        string releasePath = Path.Combine(uiProjectBase, "bin", "Release", "net10.0-windows10.0.19041.0", "win-x64", uiExeFullName);


        if (File.Exists(productionPath))
        {
            return productionPath;
        }
        else if (File.Exists(debugPath))
        {
            return debugPath;
        }
        else if (File.Exists(releasePath))
        {
            return releasePath;
        }
        
        return null; // Ejecutable no encontrado en ninguna de las rutas esperadas.
    }

    /// <summary>
    /// Actualiza el estado del WorkerService en el menú contextual.
    /// Habilita o deshabilita las opciones de iniciar/detener según el estado actual del servicio.
    /// </summary>
    private void UpdateServiceStatus()
    {
        // Ejecutar en el hilo de UI para actualizar ToolStripMenuItems de forma segura.
        Application.Current.Dispatcher.Invoke(() =>
        {
            ToolStripMenuItem statusMenuItem = (ToolStripMenuItem)_icon.ContextMenuStrip.Items["ServiceStatusMenuItem"];
            ToolStripMenuItem startMenuItem = (ToolStripMenuItem)_icon.ContextMenuStrip.Items["StartServiceMenuItem"];
            ToolStripMenuItem stopMenuItem = (ToolStripMenuItem)_icon.ContextMenuStrip.Items["StopServiceMenuItem"];

            // Primero, verificar si el proceso del worker está corriendo. Esto es lo que 'Iniciar' lanzará.
            bool isWorkerProcessRunning = Process.GetProcessesByName(WorkerExeName).Any();

            string statusText = "Estado: Desconocido";
            string statusIndicator = "⚪"; // White circle (unknown)
            ServiceControllerStatus windowsServiceStatus = ServiceControllerStatus.Stopped; // Valor por defecto

            bool isWindowsServiceInstalled = false;

            try
            {
                _windowsServiceController.Refresh();
                windowsServiceStatus = _windowsServiceController.Status;
                isWindowsServiceInstalled = true;
            }
            catch (InvalidOperationException)
            {
                // El servicio de Windows no está instalado/encontrado/accesible.
                isWindowsServiceInstalled = false;
            }
            catch (Exception ex)
            {
                // Otros errores al intentar acceder al ServiceController.
                statusText = "Estado: Error al consultar Servicio Windows";
                statusIndicator = "❌"; // Cross mark (error)
                if (statusMenuItem != null) statusMenuItem.Text = $"{statusIndicator} {statusText}";
                if (startMenuItem != null) startMenuItem.Enabled = false;
                if (stopMenuItem != null) stopMenuItem.Enabled = false;
                System.Windows.MessageBox.Show($"Error inesperado al consultar el Servicio Windows: {ex.Message}", "Appsiel Print Manager", MessageBoxButton.OK, MessageBoxImage.Error);
                return; // Salir, ya que hubo un error grave al consultar el servicio.
            }

            // --- Lógica para determinar el estado efectivo (texto e icono) ---
            if (isWorkerProcessRunning)
            {
                statusText = "En Ejecución";
                statusMenuItem.Image = _iconStatusRunning;
            }
            else // Si el proceso no está corriendo, el estado es Detenido
            {
                statusText = "Detenido";
                statusMenuItem.Image = _iconStatusStopped;
            }

            // Actualizar el texto del elemento de menú.
            if (statusMenuItem != null)
            {
                statusMenuItem.Text = $"Estado: {statusText}";
            }

            // --- Lógica para habilitar/deshabilitar los botones ---
            // La lógica para los botones sigue siendo más compleja para ofrecer un control robusto.
            if (startMenuItem != null)
            {
                // Habilitar 'Iniciar' si NO hay proceso corriendo Y (el servicio de Windows está detenido O no está instalado).
                startMenuItem.Enabled = !isWorkerProcessRunning && (windowsServiceStatus == ServiceControllerStatus.Stopped || !isWindowsServiceInstalled);
            }

            if (stopMenuItem != null)
            {
                // Habilitar 'Detener' si hay un proceso corriendo O (el servicio de Windows está en ejecución/pausado).
                stopMenuItem.Enabled = isWorkerProcessRunning || (isWindowsServiceInstalled && (windowsServiceStatus == ServiceControllerStatus.Running || windowsServiceStatus == ServiceControllerStatus.Paused));
            }
        });
    }

    /// <summary>
    /// Método invocado al seleccionar "Abrir UI" en el menú contextual.
    /// Contendrá la lógica para lanzar la interfaz de usuario MAUI.
    /// </summary>
    private void OpenUI()
    {
        try
        {
            // Verificar si la UI MAUI ya está en ejecución.
            if (Process.GetProcessesByName(MauiUIExeName).Any())
            {
                System.Windows.MessageBox.Show("La UI de Appsiel Print Manager ya está en ejecución.", "Appsiel Print Manager", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string uiExePath = FindMauiUIExecutablePath();
            if (string.IsNullOrEmpty(uiExePath))
            {
                System.Windows.MessageBox.Show("No se pudo encontrar la ruta del ejecutable de la UI de Appsiel Print Manager.", "Appsiel Print Manager", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = uiExePath,
                UseShellExecute = true // Para UI, generalmente es deseable que Windows lo lance con el shell (muestra ventana).
            });
            System.Windows.MessageBox.Show("UI de Appsiel Print Manager iniciada.", "Appsiel Print Manager", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error inesperado al abrir la UI de Appsiel Print Manager: {ex.Message}", "Appsiel Print Manager", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Método invocado al seleccionar "Iniciar WorkerService" en el menú contextual.
    /// Lanza el ejecutable del WorkerService como un proceso.
    /// </summary>
    private void StartWorkerService()
    {
        try
        {
            if (Process.GetProcessesByName(WorkerExeName).Any())
            {
                System.Windows.MessageBox.Show("WorkerService ya está en ejecución como proceso.", "Appsiel Print Manager", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string workerExePath = FindWorkerServiceExecutablePath();
            if (string.IsNullOrEmpty(workerExePath))
            {
                System.Windows.MessageBox.Show("No se pudo encontrar la ruta del ejecutable del WorkerService.", "Appsiel Print Manager", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = workerExePath,
                UseShellExecute = false, // No usar el shell para la ejecución
                RedirectStandardOutput = true, // Redirigir la salida estándar
                RedirectStandardError = true,  // Redirigir el error estándar
                CreateNoWindow = true          // No crear una ventana para el proceso
            });
            System.Windows.MessageBox.Show("WorkerService iniciado como proceso. Verifique su estado.", "Appsiel Print Manager", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error inesperado al iniciar el WorkerService: {ex.Message}", "Appsiel Print Manager", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            UpdateServiceStatus(); // Siempre actualiza el estado después de intentar iniciar.
        }
    }

    /// <summary>
    /// Método invocado al seleccionar "Detener WorkerService" en el menú contextual.
    /// Detiene el proceso del WorkerService.
    /// </summary>
    private void StopWorkerService()
    {
        try
        {
            Process[] workerProcesses = Process.GetProcessesByName(WorkerExeName);
            if (workerProcesses.Length == 0)
            {
                System.Windows.MessageBox.Show("WorkerService no está en ejecución como proceso para detenerlo.", "Appsiel Print Manager", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            foreach (var process in workerProcesses)
            {
                process.Kill();
                process.WaitForExit(5000); // Esperar un máximo de 5 segundos para que el proceso termine
            }
            System.Windows.MessageBox.Show("WorkerService detenido correctamente.", "Appsiel Print Manager", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error inesperado al detener el WorkerService: {ex.Message}", "Appsiel Print Manager", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            UpdateServiceStatus(); // Siempre actualiza el estado después de intentar detener.
        }
    }

    /// <summary>
    /// Método invocado al seleccionar "Salir" en el menú contextual.
    /// Detiene el temporizador, libera recursos y cierra la aplicación.
    /// </summary>
    private void Exit()
    {
        // 1. Detener el WorkerService (proceso) si está en ejecución
        try
        {
            Process[] workerProcesses = Process.GetProcessesByName(WorkerExeName);
            if (workerProcesses.Length > 0)
            {
                System.Windows.MessageBox.Show("Deteniendo WorkerService...", "Appsiel Print Manager", MessageBoxButton.OK, MessageBoxImage.Information);
                foreach (var process in workerProcesses)
                {
                    process.Kill();
                    process.WaitForExit(5000);
                }
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error al detener WorkerService al salir: {ex.Message}", "Appsiel Print Manager", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        // 2. Cerrar la UI MAUI si está en ejecución
        try
        {
            Process[] uiProcesses = Process.GetProcessesByName(MauiUIExeName);
            if (uiProcesses.Length > 0)
            {
                System.Windows.MessageBox.Show("Cerrando UI de Appsiel Print Manager...", "Appsiel Print Manager", MessageBoxButton.OK, MessageBoxImage.Information);
                foreach (var process in uiProcesses)
                {
                    process.Kill();
                    process.WaitForExit(5000);
                }
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error al cerrar la UI al salir: {ex.Message}", "Appsiel Print Manager", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        // Detiene y libera los recursos del temporizador.
        _statusUpdateTimer.Stop();
        _statusUpdateTimer.Dispose();
        // Libera los recursos del NotifyIcon.
        _icon.Dispose();
        // Cierra la aplicación WPF actual.
        System.Windows.Application.Current.Shutdown();
    }

    /// <summary>
    /// Implementación de IDisposable para liberar recursos gestionados.
    /// </summary>
    public void Dispose()
    {
        // Libera los recursos del temporizador si no son nulos.
        _statusUpdateTimer?.Dispose();
        // Libera los recursos del NotifyIcon si no son nulos.
        _icon?.Dispose();
        _iconStatusRunning?.Dispose();
        _iconStatusStopped?.Dispose();
        _iconActionStart?.Dispose();
        _iconActionStop?.Dispose();
    }
}