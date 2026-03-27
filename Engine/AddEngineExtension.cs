using Infrastructure.Logging;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Engine;

public static class AddEngineExtension
{
    /// <summary>
    /// Registra los servicios de Engine en el contenedor DI
    /// </summary>
    /// <param name="services">Contenedor de servicios</param>
    /// <returns>El contenedor de servicios actualizado</returns>
    public static IServiceCollection AddEngineServices(this IServiceCollection services, string rutaLogs)
    {
        // Registramos MigrationService
        // Usamos factory para inyectar ambos loggers desde el contenedor
        services.AddSingleton<Services.MigrationService>(sp =>
        {
            var mdWriter = sp.GetRequiredService<ILogWriterMD>();
            var jsonWriter = sp.GetRequiredService<ILogWriterJSON>();
            return new Services.MigrationService(mdWriter, jsonWriter, rutaLogs);
        });

        return services;
    }
}