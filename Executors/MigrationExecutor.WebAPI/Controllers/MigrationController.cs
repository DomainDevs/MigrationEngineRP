using Core.Entities;
using DocumentFormat.OpenXml.Wordprocessing;
using Engine.Services;
using Infrastructure.Config;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MigrationExecutor.WebAPI.Utils;
using System.ComponentModel;

namespace MigrationExecutor.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MigrationController : ControllerBase
{
    private readonly MigrationService _migrationService;
    private readonly MigrationConfig _config;

    public MigrationController(MigrationService migrationService, IOptions<MigrationConfig> config)
    {
        _migrationService = migrationService ?? throw new ArgumentNullException(nameof(migrationService));
        _config = config.Value;
    }


    [HttpPost("run-job-from-folderm")]
    public IActionResult EjecutarJobDesdeCarpeta([FromBody] JobRequest request)
    {
        try
        {
            _migrationService.EjecutarJobDesdeCarpeta(
                nombreJob: request.Nombre,
                carpetaPaquetes: _config.CarpetaPaquetes,
                SourceDB: _config.SourceDB,
                DestinationDB: _config.DestinationDB,
                paquetesIncluir: request.PaquetesIncluir,
                paquetesOmitir: request.PaquetesOmitir
            );

            return Ok(new
            {
                Mensaje = "Job ejecutado correctamente",
                Job = request.Nombre
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                Mensaje = "Error al ejecutar el job",
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// Endpoint de prueba de salud, verificar que la API funciona
    /// </summary>
    [HttpGet("ping")]
    public IActionResult Ping() => Ok("MigrationExecutor WebAPI activa!!.");
}


public class JobRequest
{
    [DefaultValue("MigracionDinamica")]
    public string Nombre { get; set; } = "MigracionDinamica";

    [DefaultValue("[]")]
    public List<string>? PaquetesIncluir { get; set; }

    [DefaultValue("[]")]
    public List<string>? PaquetesOmitir { get; set; }
}