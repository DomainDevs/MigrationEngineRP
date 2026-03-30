using Core.Entities;
using Engine.Extensions;
using Engine.Hubs;
using Infrastructure.Config;
using Infrastructure.Logging;
using Infrastructure.Reporting;
using Microsoft.AspNetCore.SignalR;

namespace Engine.Services
{
    public class MigrationService
    {
        private readonly ILogWriterMD _mdWriter;
        private readonly ILogWriterJSON _jsonWriter;
        private readonly MigrationReportExcelWriter _reportWriter;

        private static int _isRunning = 0;
        private static readonly object _lock = new object();
        private static MigrationJob? _currentJob;

        private readonly MigrationConfig _config;

        private readonly IHubContext<MigrationHub> _hubContext; //Hub

        public MigrationService(ILogWriterMD mdWriter, ILogWriterJSON jsonWriter, string reportsOutputFolder, MigrationConfig config,
            IHubContext<MigrationHub> hubContext
            )
        {
            _mdWriter = mdWriter ?? throw new ArgumentNullException(nameof(mdWriter));
            _jsonWriter = jsonWriter ?? throw new ArgumentNullException(nameof(jsonWriter));
            _reportWriter = new MigrationReportExcelWriter(reportsOutputFolder);
            _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
            _config = config;
        }

        public bool EjecutarJob(MigrationJob job)
        {
            lock (_lock)
            {
                if (Interlocked.CompareExchange(ref _isRunning, 1, 0) != 0)
                    return false;

                _currentJob = job;
            }

            try
            {
                job.FechaEjecucion = DateTime.Now;

                // Crear nombre de reporte
                string excelFileName = ReportGeneratorExtensions.GetExcelFileName(job, _reportWriter.OutputFolder);
                
                // Inicializar logs
                var logs = MigrationServiceExtensions.InicializarLogs(job, excelFileName);

                // Ejecutar pasos
                logs = MigrationServiceExtensions.EjecutarPasos(job, logs, _hubContext, _config);

                job.Completado = job.Pasos.All(p => p.Exito);

                // Generar DTO para reportes
                var reportDto = ReportGeneratorExtensions.MapToReportDto(job);

                // Generar reportes JSON, MD y Excel
                ReportGeneratorExtensions.EscribirReportes(job, logs, reportDto, excelFileName, _jsonWriter, _mdWriter, _reportWriter);

                return true;
            }
            finally
            {
                lock (_lock)
                {
                    _currentJob = null;
                    Interlocked.Exchange(ref _isRunning, 0);
                }
            }
        }

        public void EjecutarJobDesdeCarpeta(
            string nombreJob,
            string carpetaPaquetes,
            string SourceDB,
            string DestinationDB,
            List<string>? paquetesIncluir = null,
            List<string>? paquetesOmitir = null)
        {
            var job = MigrationServiceExtensions.CrearJobDesdeCarpeta(nombreJob, carpetaPaquetes, SourceDB, DestinationDB,
                paquetesIncluir, paquetesOmitir);


            EjecutarJob(job);
        }

        public MigrationJob? GetCurrentJob() => _currentJob;
    }
}