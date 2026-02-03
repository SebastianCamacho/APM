using AppsielPrintManager.Core.Interfaces;
using AppsielPrintManager.Infraestructure.Repositories;
using AppsielPrintManager.Infraestructure.Services;
using Microsoft.Extensions.DependencyInjection;
using WorkerService;
using Microsoft.Extensions.Hosting.WindowsServices; // Added for UseWindowsService()

var builder = Host.CreateDefaultBuilder(args) // Changed to CreateDefaultBuilder
    .UseWindowsService() // Configure the host to run as a Windows Service
    .ConfigureServices((hostContext, services) =>
    {
        // Registro de servicios de Core e Infraestructure
        services.AddSingleton<ILoggingService, Logger>();
        services.AddSingleton<IWebSocketService, WebSocketServerService>();
        services.AddSingleton<ISettingsRepository, SettingsRepository>();
        services.AddSingleton<ITemplateRepository, TemplateRepository>(); // Agregado para resolver dependencia en TicketRendererService
        services.AddSingleton<ITicketRenderer, TicketRendererService>();
        services.AddSingleton<IEscPosGenerator, EscPosGeneratorService>();
        services.AddSingleton<TcpIpPrinterClient>();
        services.AddSingleton<IPrintService, PrintService>();

        // Scale Services
        services.AddSingleton<IScaleRepository, JsonScaleRepository>();
        services.AddSingleton<IScaleService, SerialScaleService>();

        services.AddHostedService<Worker>();
    });

var host = builder.Build();
host.Run();
