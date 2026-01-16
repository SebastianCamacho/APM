using System.Windows;
using TrayApp.Services; // Importar el namespace del servicio

namespace TrayApp;

public partial class App : System.Windows.Application
{
    private TrayIcon _tray;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        _tray = new TrayIcon();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _tray?.Dispose();
        base.OnExit(e);
    }
}