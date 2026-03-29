using Engine;
using Engine.Hubs;
using Infrastructure;
using Microsoft.Extensions.FileProviders;
using MigrationExecutor.WebAPI.Utils;

var builder = WebApplication.CreateBuilder(args);

// Cargar configuración *.json
builder.Configuration.AddJsonFile("Configurations/appsettings.json", optional: false, reloadOnChange: true);
builder.Configuration.AddJsonFile("Configurations/documentation.json", optional: false, reloadOnChange: true);
builder.Configuration.AddJsonFile("Configurations/cors.json", optional: false, reloadOnChange: true);

string basePath = AppContext.BaseDirectory; // ruta del exe o WebAPI
string rutaLogs = Path.Combine(basePath, builder.Configuration.GetValue<string>("Migration:CarpetaLogs"));

builder.Services.AddInfrastructureServices(builder.Configuration, rutaLogs, true); //Registrar Infraestructura
builder.Services.AddEngineServices(builder.Configuration, rutaLogs); // Registrar Engine

//builder.Services.AddControllers(); // Registrar controladores
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

// Mostrar archivos
app.UseFileServer(new FileServerOptions
{
    FileProvider = new PhysicalFileProvider(rutaLogs),
    RequestPath = "/LogMigration",
    EnableDirectoryBrowsing = true
});
app.UseDirectoryBrowser();
app.MapControllers();
app.Run();