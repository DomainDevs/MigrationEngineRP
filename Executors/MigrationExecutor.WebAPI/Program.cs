using Core.Entities;
using Engine;
using Engine.Services;
using Infrastructure;
using Infrastructure.Documentation; // <- para AddConfiguredSwagger
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MigrationExecutor.WebAPI.Utils;

var builder = WebApplication.CreateBuilder(args);

// Cargar configuración *.json
builder.Configuration.AddJsonFile("Configurations/appsettings.json", optional: false, reloadOnChange: true);
builder.Configuration.AddJsonFile("Configurations/documentation.json", optional: false, reloadOnChange: true);
builder.Configuration.AddJsonFile("Configurations/cors.json", optional: false, reloadOnChange: true);

// Para poder inyectar directamente MigrationConfig, opcionalmente:
builder.Services.Configure<MigrationConfig>(builder.Configuration.GetSection("Migration"));

// Recupera carpeta de logs
string basePath = AppContext.BaseDirectory; // ruta del exe o WebAPI
string rutaLogs = Path.Combine(basePath, builder.Configuration.GetValue<string>("Migration:CarpetaLogs"));

builder.Services.AddInfrastructureServices(builder.Configuration, rutaLogs, true); //Registrar Infraestructura
builder.Services.AddEngineServices(rutaLogs); // Registrar Engine
//builder.Services.AddControllers(); // Registrar controladores

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR(); ///Adicionar AddSignalR

builder.Services.AddDirectoryBrowser(); // mostrar archivos

var app = builder.Build();
app.UseInfrastructure(builder.Configuration);

// Middleware
app.UseRouting();

// Mapea tu Hub
app.MapHub<MigrationHub>("/migrationHub");


// mostrar archivos
app.UseFileServer(new FileServerOptions
{
    FileProvider = new PhysicalFileProvider(rutaLogs),
    RequestPath = "/LogMigration",
    EnableDirectoryBrowsing = true
});
app.UseDirectoryBrowser();

app.MapControllers();

app.Run();