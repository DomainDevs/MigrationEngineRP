using Engine.Hubs;
using Engine.Interface;
using Engine.Services;
using Infrastructure.Config;
using Infrastructure.Logging;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Engine;

public static class AddEngineExtension
{
    /// <summary>
    /// Registra los servicios del Engine en el contenedor DI.
    /// </summary>
    public static IServiceCollection AddEngineServices(
        this IServiceCollection services,
        IConfiguration config,
        string rutaLogs)
    {
        // CONFIGURACIÓN
        services.Configure<MigrationConfig>(
            config.GetSection("MigrationConfig"));

        // PROVIDERS (PLUGINS)
        services.AddScoped<IEtlPackageProvider, FileSystemEtlPackageProvider>();

        // CORE ENGINE SERVICES
        services.AddSingleton<MigrationService>(sp =>
        {
            return CreateMigrationService(sp, rutaLogs);
        });

        return services;
    }

    /// <summary>
    /// Factory aislado para construcción del core engine.
    /// Mejora legibilidad y testabilidad del composition root.
    /// </summary>
    private static MigrationService CreateMigrationService(
        IServiceProvider sp,
        string rutaLogs)
    {
        var mdWriter = sp.GetRequiredService<ILogWriterMD>();
        var jsonWriter = sp.GetRequiredService<ILogWriterJSON>();

        var migrationConfig = sp
            .GetRequiredService<IOptions<MigrationConfig>>()
            .Value;

        var hubContext = sp
            .GetRequiredService<IHubContext<MigrationHub>>();

        return new MigrationService(
            mdWriter,
            jsonWriter,
            rutaLogs,
            migrationConfig,
            hubContext);
    }
}