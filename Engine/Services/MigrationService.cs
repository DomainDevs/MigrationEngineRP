using Core.Entities;
using DocumentFormat.OpenXml.Spreadsheet;
using Infrastructure.DTOs;
using Infrastructure.Logging;
using Infrastructure.Reporting;

namespace Engine.Services
{
    public class MigrationService
    {
        private readonly ILogWriterMD _mdWriter;
        private readonly ILogWriterJSON _jsonWriter;
        private readonly MigrationReportExcelWriter _reportWriter;

        // Estado global
        private static int _isRunning = 0;
        private static readonly object _lock = new object();
        private static MigrationJob? _currentJob;

        public MigrationService(ILogWriterMD mdWriter, ILogWriterJSON jsonWriter, string reportsOutputFolder)
        {
            _mdWriter = mdWriter ?? throw new ArgumentNullException(nameof(mdWriter));
            _jsonWriter = jsonWriter ?? throw new ArgumentNullException(nameof(jsonWriter));

            // Instanciamos el escritor de reportes usando la carpeta de salida
            _reportWriter = new MigrationReportExcelWriter(reportsOutputFolder);
        }

        private LogEntry EjecutarPaso(MigrationStep step)
        {
            step.Inicio = DateTime.Now;
            var logEntry = new LogEntry { NombrePaso = step.Nombre, Inicio = step.Inicio };

            try
            {
                // Aquí se ejecutaría el paquete SSIS o lógica del paso
                step.Exito = true;
                step.Mensaje = "Paso ejecutado correctamente";
                logEntry.Exito = true;
                logEntry.Mensaje = step.Mensaje;

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"> {step.Nombre} [OK]");
            }
            catch (Exception ex)
            {
                step.Exito = false;
                step.Mensaje = ex.Message;
                logEntry.Exito = false;
                logEntry.Mensaje = ex.Message;

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"> {step.Nombre} [FAIL]");
            }
            finally
            {
                Console.ResetColor();
                step.Fin = DateTime.Now;
                logEntry.Fin = step.Fin;
            }

            return logEntry;
        }

        public bool EjecutarJob(MigrationJob job)
        {
            if (job == null) throw new ArgumentNullException(nameof(job));
            if (job.Pasos == null || job.Pasos.Count == 0) return false;

            lock (_lock)
            {
                if (Interlocked.CompareExchange(ref _isRunning, 1, 0) != 0)
                    return false; // Ya hay un job ejecutándose

                _currentJob = job;
            }

            try
            {
                job.FechaEjecucion = DateTime.Now;

                // ======================== 🔹 Crear nombre de Excel al inicio 🔹 ========================
                string excelFileName = $"MigrationReport_{job.Id}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                string excelPath = Path.Combine(_reportWriter.OutputFolder, excelFileName);
                var jobExcelPath = excelPath;

                // ======================== 🔹 Inicializar logs con FileXLS 🔹 ========================
                var logs = new List<LogEntry>();
                foreach (var step in job.Pasos)
                {
                    logs.Add(new LogEntry
                    {
                        NombrePaso = step.Nombre,
                        Inicio = step.Inicio,
                        FileXLS = excelFileName // Aquí ya tenemos el nombre asignado
                    });
                }

                int totalSteps = job.Pasos.Count;

                // ======================== 🔹 Ejecutar cada paso 🔹 ========================
                for (int i = 0; i < totalSteps; i++)
                {
                    var step = job.Pasos[i];
                    var log = EjecutarPaso(step);

                    // Mantener FileXLS en cada log
                    log.FileXLS = excelFileName;
                    logs[i] = log;

                    double progress = ((i + 1) * 100.0) / totalSteps;
                }

                job.Completado = job.Pasos.All(p => p.Exito);

                // ======================== 🔹 Mapear a DTO para Excel 🔹 ========================
                var reportDto = new MigrationJobReportDto
                {
                    JobId = job.Id.ToString(),
                    Steps = job.Pasos.Select((s, index) => new MigrationStepReportDto
                    {
                        StepId = index + 1,
                        StepName = s.Nombre,
                        Status = s.Exito ? "Success" : "Failed",
                        RowsProcessed = 0,
                        StartTime = s.Inicio,
                        EndTime = s.Fin,
                        DurationSeconds = (s.Fin - s.Inicio).TotalSeconds,
                        Message = s.Mensaje
                    }).ToList()
                };

                // ======================== 🔹 Escribir JSON y MD antes de generar Excel 🔹 ========================
                _jsonWriter.EscribirLog(job.Nombre, logs);
                _mdWriter.EscribirLog(job.Nombre, logs);

                MostrarResumen(job, logs);

                // ======================== 🔹 Generar Excel 🔹 ========================
                _reportWriter.WriteJobReport(reportDto, excelPath);
                Console.WriteLine($"Reporte Excel generado: {excelPath}");

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

        public MigrationJob? GetCurrentJob() => _currentJob;

        public void EjecutarJobDesdeCarpeta(
            string nombreJob,
            string carpetaPaquetes,
            List<string>? paquetesIncluir = null,
            List<string>? paquetesOmitir = null)
        {
            if (string.IsNullOrWhiteSpace(nombreJob)) throw new ArgumentException("Nombre del job requerido.", nameof(nombreJob));
            if (!Directory.Exists(carpetaPaquetes)) throw new DirectoryNotFoundException($"La carpeta {carpetaPaquetes} no existe.");

            var archivosDisponibles = Directory.GetFiles(carpetaPaquetes, "*.dtsx");
            var nombresDisponibles = archivosDisponibles.Select(f => Path.GetFileName(f)).ToList();

            if (paquetesOmitir != null && paquetesOmitir.Count > 0)
            {
                var omitidosNoEncontrados = paquetesOmitir
                    .Where(p => !nombresDisponibles.Contains(p, StringComparer.OrdinalIgnoreCase))
                    .ToList();

                if (omitidosNoEncontrados.Any())
                    throw new FileNotFoundException($"Paquetes a omitir no encontrados: {string.Join(", ", omitidosNoEncontrados)}");
            }

            if (paquetesIncluir != null && paquetesIncluir.Count > 0)
            {
                var paquetesNoEncontrados = paquetesIncluir
                    .Where(p => !nombresDisponibles.Contains(p, StringComparer.OrdinalIgnoreCase))
                    .ToList();

                if (paquetesNoEncontrados.Any())
                    throw new FileNotFoundException($"Paquetes a incluir no encontrados: {string.Join(", ", paquetesNoEncontrados)}");

                archivosDisponibles = archivosDisponibles
                    .Where(f => paquetesIncluir.Contains(Path.GetFileName(f), StringComparer.OrdinalIgnoreCase))
                    .ToArray();
            }

            if (paquetesOmitir != null && paquetesOmitir.Count > 0)
            {
                archivosDisponibles = archivosDisponibles
                    .Where(f => !paquetesOmitir.Contains(Path.GetFileName(f), StringComparer.OrdinalIgnoreCase))
                    .ToArray();
            }

            if (archivosDisponibles.Length == 0)
                throw new InvalidOperationException("No hay paquetes válidos para ejecutar.");

            var pasos = archivosDisponibles
                .Select(a => new MigrationStep { Nombre = Path.GetFileNameWithoutExtension(a), RutaPaquete = a })
                .ToList();

            var job = new MigrationJob
            {
                Nombre = nombreJob,
                Pasos = pasos,
                FechaEjecucion = DateTime.Now
            };

            EjecutarJob(job);
        }

        private void MostrarResumen(MigrationJob job, List<LogEntry> logs)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"\n====== Resumen Job: {job.Nombre} ======");
            Console.ResetColor();

            int exitos = logs.Count(l => l.Exito);
            int fallidos = logs.Count - exitos;

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Total pasos: {logs.Count}");
            Console.WriteLine($"Éxitos: {exitos}");
            Console.WriteLine($"Fallidos: {fallidos}");
            Console.ResetColor();

            Console.ForegroundColor = job.Completado ? ConsoleColor.Green : ConsoleColor.Red;
            Console.WriteLine($"\nEstado final del Job: {(job.Completado ? "[OK]" : "[FAIL]")}");
            Console.ResetColor();
        }
    }
}