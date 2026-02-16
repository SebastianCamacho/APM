using System.Windows;
using TrayApp.Services; // Importar el namespace del servicio

namespace TrayApp;

public partial class App : System.Windows.Application
{
    private TrayIcon _tray;

    protected override void OnStartup(StartupEventArgs e)
    {
        try
        {
            base.OnStartup(e);
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            _tray = new TrayIcon();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error fatal al iniciar TrayApp: {ex.Message}\n\nLa aplicación se cerrará.",
                            "Error de Appsiel Print Manager",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
            Shutdown(-1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _tray?.Dispose();
        base.OnExit(e);
    }
}