using Core.Entities;
using Engine.Services;
using MigrationExecutor.WebAPI.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace MigrationExecutor.WebAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MigrationProgressController : ControllerBase
    {
        private readonly MigrationService _migrationService;
        private readonly IHubContext<MigrationHub> _hubContext;

        public MigrationProgressController(MigrationService migrationService, IHubContext<MigrationHub> hubContext)
        {
            _migrationService = migrationService;
            _hubContext = hubContext;

            // Suscribirse al evento de progreso
            _migrationService.OnProgressChanged += async (sender, args) =>
            {
                if (args?.Job == null) return;

                await _hubContext.Clients.All.SendAsync("ReceiveProgress", new
                {
                    StepName = args.StepName,
                    Percentage = args.Progress,
                    JobName = args.Job.Nombre
                });
            };
        }

        [HttpPost("start")]
        public IActionResult StartMigration([FromBody] StartMigrationRequest request)
        {
            var job = _migrationService.GetCurrentJob();
            if (job != null)
                return Conflict("Ya hay una migración en curso.");

            // Ejecuta la migración en otro hilo para no bloquear el request
            Task.Run(() => _migrationService.EjecutarJob(request.Job));

            return Ok("Migración iniciada");
        }
    }

    public class StartMigrationRequest
    {
        public MigrationJob Job { get; set; } = new MigrationJob();
    }
}