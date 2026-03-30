using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MigrationExecutor.Console.Utils;
using Engine.Services;
using Infrastructure;
using Engine;

try
{
    Console.OutputEncoding = System.Text.Encoding.UTF8;
    // Banner inicial
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine("=============================================");
    Console.WriteLine("       MOTOR DE MIGRACIÓN ETL V 1.0.0        ");
    Console.WriteLine("            SISTRAN 2026/03/03               ");
    Console.WriteLine("=============================================");
    Console.ResetColor();

    Console.WriteLine("Iniciando migración...");

    // Crear host y cargar configuración desde Config/
    var host = Host.CreateDefaultBuilder(args)
        // 🔹 Logging limpio
        .ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Warning);
        })
        .AddConfigurations() // carga JSON de Config/ y variables de entorno
        .ConfigureServices((context, services) =>
        {
            var migrationConfig = context.Configuration.GetSection("Migration").Get<MigrationConfig>();

            // Ruta de logs relativa al exe
            string logsPath = Path.Combine(AppContext.BaseDirectory, migrationConfig.CarpetaLogs);
            Directory.CreateDirectory(logsPath);

            services.AddInfrastructureServices(context.Configuration, logsPath);
            services.AddEngineServices(context.Configuration, logsPath);
        })
        .Build();

    // Resolver servicio principal
    var migrationService = host.Services.GetRequiredService<MigrationService>();

    // Obtener configuración de Migration para ejecución
    var migrationConfigRoot = host.Services.GetRequiredService<IConfiguration>();
    var migrationConfigObj = migrationConfigRoot.GetSection("Migration").Get<MigrationConfig>();

    // Banner de ejecución
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine($"\n[Information] Ejecutando Job: {migrationConfigObj.NombreJob}");
    Console.ResetColor();

    // Ejecutar job dinámico desde carpeta
    migrationService.EjecutarJobDesdeCarpeta(
        migrationConfigObj.NombreJob,
        migrationConfigObj.CarpetaPaquetes,
        null, null,
        migrationConfigObj.PaquetesIncluir,
        migrationConfigObj.PaquetesOmitir
    );

    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine($"[Information] Carpeta de paquetes: {migrationConfigObj.CarpetaPaquetes}");
    Console.WriteLine($"[Information] Carpeta de logs: {migrationConfigObj.CarpetaLogs}");
    Console.ResetColor();

    // Banner final
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("\n[OK] Proceso de Migración Finalizado!!");
    Console.ResetColor();

    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine("\nPresione cualquier tecla para salir...");
    Console.ResetColor();
    Console.ReadKey();
}
catch (Exception ex) when (!ex.GetType().Name.Equals("StopTheHostException", StringComparison.Ordinal))
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("\n[ERROR] Proceso de Migración Fallido:" + ex.Message);
    Console.ResetColor();
}