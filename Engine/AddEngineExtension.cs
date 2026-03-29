using Engine.Hubs;
using Infrastructure.Config;
using Infrastructure.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;


namespace Engine;

public static class AddEngineExtension
{
    /// <summary>
    /// Registra los servicios de Engine en el contenedor DI
    /// </summary>
    /// <param name="services">Contenedor de servicios</param>
    /// <returns>El contenedor de servicios actualizado</returns>
    public static IServiceCollection AddEngineServices(this IServiceCollection services, IConfiguration config, string rutaLogs)
    {

        // Registramos MigrationService
        // Usamos factory para inyectar ambos loggers desde el contenedor
        services.AddSingleton<Services.MigrationService>(sp =>
        {
            var mdWriter = sp.GetRequiredService<ILogWriterMD>();
            var jsonWriter = sp.GetRequiredService<ILogWriterJSON>();

            var options = sp.GetRequiredService<IOptions<MigrationConfig>>();
            var migrationConfig = options.Value;

            var hubContext = sp.GetRequiredService<IHubContext<MigrationHub>>(); // <--- inyección
            return new Services.MigrationService(mdWriter, jsonWriter, rutaLogs, migrationConfig, hubContext);
        });

        return services;
    }
}