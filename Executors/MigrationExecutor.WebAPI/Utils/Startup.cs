
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Infrastructure;
using Engine;

namespace MigrationExecutor.WebAPI.Utils;

public static class Startup
{
    public static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Carpeta de logs
        var rutaLogs = configuration.GetValue<string>("Migration:CarpetaLogs");
        services.AddInfrastructureServices(configuration, rutaLogs);   // Infrastructure

        // Swagger
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
    }
    public static void Configure(WebApplication app)
    {

        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "MigrationExecutor API v1");
            c.RoutePrefix = "swagger";
        });


        //app.UseHttpsRedirection(); // 3. Fuerza HTTPS
        app.UseAuthorization();
        app.MapControllers();
    }
}
