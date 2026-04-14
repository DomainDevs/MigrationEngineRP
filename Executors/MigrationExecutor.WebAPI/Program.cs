using Engine;
using Engine.Hubs;
using Infrastructure;
using Microsoft.Extensions.FileProviders;
using MigrationExecutor.WebAPI.Utils;
using Infrastructure.Common.Diagnostics;

Console.OutputEncoding = System.Text.Encoding.UTF8;
try
{
    BootConsole.WriteBanner("[Sistran Migration Executor]");

    var builder = WebApplication.CreateBuilder(args);

    
    BootConsole.Step("1/5", "Configurando Host y Serilog..."); // Cargar configuración *.json
    builder.Configuration.AddJsonFile("Configurations/appsettings.json", optional: false, reloadOnChange: true);
    builder.Configuration.AddJsonFile("Configurations/documentation.json", optional: false, reloadOnChange: true);
    builder.Configuration.AddJsonFile("Configurations/cors.json", optional: false, reloadOnChange: true);

    string basePath = AppContext.BaseDirectory; // ruta del exe o WebAPI
    string rutaLogs = Path.Combine(basePath, builder.Configuration.GetValue<string>("Migration:CarpetaLogs"));

    
    BootConsole.Step("2/5", "Inyectando dependencias y escaneando assemblies (Scrutor)...");
    builder.Services
        .AddInfrastructureServices(builder.Configuration, rutaLogs, true) //Registrar Infraestructura
        .AddEngineServices(builder.Configuration, rutaLogs) // Registrar Engine
        .AddEndpointsApiExplorer()
        .AddSwaggerGen()
        .AddSignalR(); ///Adicionar AddSignalR

    builder.Services.AddDirectoryBrowser(); // mostrar archivos

    BootConsole.Step("3/5", "Construyendo ServiceProvider y contenedor...");
    var app = builder.Build();

    BootConsole.Step("4/5", "Configurando Pipeline HTTP y middleware de seguridad...");
    app.UseInfrastructure(builder.Configuration);

    // Middleware
    app.UseRouting();

    // Mapea tu Hub
    app.MapHub<MigrationHub>("/migrationHub");

    // Mostrar archivos
    app.UseFileServer(new FileServerOptions
    {
        FileProvider = new PhysicalFileProvider(rutaLogs),
        RequestPath = "/LogMigration",
        EnableDirectoryBrowsing = true
    });
    app.UseDirectoryBrowser();
    app.MapControllers();

    BootConsole.Step("5/5", "¡Servicio desplegado con éxito!");
    app.Run();
}
catch (Exception ex) when (!ex.GetType().Name.Equals("StopTheHostException", StringComparison.Ordinal))
{
    StartupDiagnostics.LogStartupError(ex);
}
finally
{
    //Log.Information(">> Servicio finalizado y recursos liberados <<");
    //Log.CloseAndFlush();
}


internal static class BootConsole
{
    public static void WriteBanner(string name)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"\n » INICIANDO SERVICIO {name}...");
        Console.ResetColor();
    }

    public static void Step(string step, string msg)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write($" Ok [{step}] ");
        Console.ResetColor();
        Console.WriteLine(msg);
    }
}