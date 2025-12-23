using AppsielPrintManager.Core.Interfaces;
using AppsielPrintManager.Infraestructure.Services;
using AppsielPrintManager.Infraestructure.Repositories; // Nuevo using
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;

namespace ConsoleApp
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            await CreateHostBuilder(args).Build().RunAsync();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    // Registro de servicios de Core e Infraestructure
                    services.AddSingleton<ILoggingService, Logger>();
                    services.AddSingleton<IWebSocketService, WebSocketServerService>();
                    services.AddSingleton<ISettingsRepository, SettingsRepository>(); // Nuevo registro

                    // Registrar el servicio alojado que gestionará la lógica de la consola y el WebSocketServer.
                    services.AddHostedService<ConsoleHostedService>();
                })
                .UseConsoleLifetime(); // Habilita la gestión del ciclo de vida de la consola (Ctrl+C).
    }
}
