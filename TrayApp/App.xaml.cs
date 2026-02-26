using System.Windows;
using TrayApp.Services; // Importar el namespace del servicio
using AppsielPrintManager.Infraestructure.Services; // Para Logger
using AppsielPrintManager.Core.Interfaces;

namespace TrayApp;

public partial class App : System.Windows.Application
{
    private TrayIcon? _tray;
    private ILoggingService? _logger;

    protected override void OnStartup(StartupEventArgs e)
    {
        try
        {
            base.OnStartup(e);
            _logger = new Logger();
            _logger.LogInfo("Iniciando TrayApp...", "TrayApp");
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            _tray = new TrayIcon(_logger);
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Error fatal al iniciar TrayApp: {ex.Message}", ex, "TrayApp");
            System.Windows.MessageBox.Show($"Error fatal al iniciar TrayApp: {ex.Message}\n\nLa aplicación se cerrará.",
                            "Error de Appsiel Print Manager",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
            Shutdown(-1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _logger?.LogInfo("Cerrando TrayApp...", "TrayApp");
        _tray?.Dispose();
        base.OnExit(e);
    }
}