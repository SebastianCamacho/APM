using AppsielPrintManager.Core.Interfaces;
using AppsielPrintManager.Infraestructure.Repositories;
using AppsielPrintManager.Infraestructure.Services;
using Microsoft.Extensions.DependencyInjection;
using WorkerService;

var builder = Host.CreateApplicationBuilder(args);

// Registro de servicios de Core e Infraestructure
builder.Services.AddSingleton<ILoggingService, Logger>();
builder.Services.AddSingleton<IWebSocketService, WebSocketServerService>();
builder.Services.AddSingleton<ISettingsRepository, SettingsRepository>();
builder.Services.AddSingleton<ITicketRenderer, TicketRendererService>();
builder.Services.AddSingleton<IEscPosGenerator, EscPosGeneratorService>();
builder.Services.AddSingleton<TcpIpPrinterClient>();
builder.Services.AddSingleton<IPrintService, PrintService>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
