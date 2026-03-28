using Core.Entities;
using Infrastructure.DTOs;
using Infrastructure.Logging;
using Infrastructure.Reporting;

namespace Engine.Extensions
{
    /// <summary>
    /// Extension para la generación de reportes de migración.
    /// 
    /// Esta clase contiene métodos auxiliares para:
    /// 1. Crear nombres de archivo Excel para reportes.
    /// 2. Mapear un MigrationJob a un DTO listo para reporte.
    /// 3. Escribir reportes en JSON, Markdown y Excel.
    /// 
    /// Métodos estáticos y modulares
    /// Lógica de reporting y mantener el workflow limpio.
    /// </summary>
    public static class ReportGeneratorExtensions
    {
        /// <summary>
        /// Genera el nombre completo de un archivo Excel para un job de migración.
        /// Incluye el Id del job y la fecha/hora de ejecución.
        /// </summary>
        /// <param name="job">Job de migración.</param>
        /// <param name="outputFolder">Carpeta de salida donde se guardará el Excel.</param>
        /// <returns>Ruta completa del archivo Excel.</returns>
        public static string GetExcelFileName(MigrationJob job, string outputFolder)
        {
            string excelFileName = $"MigrationReport_{job.Id}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return Path.Combine(outputFolder, excelFileName);
        }

        /// <summary>
        /// Mapea un <see cref="MigrationJob"/> a un <see cref="MigrationJobReportDto"/>
        /// listo para ser exportado a Excel u otros formatos de reporte.
        /// </summary>
        /// <param name="job">Job de migración a mapear.</param>
        /// <returns>DTO de reporte con información de cada paso.</returns>
        public static MigrationJobReportDto MapToReportDto(MigrationJob job)
        {
            return new MigrationJobReportDto
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
        }

        /// <summary>
        /// Escribe los reportes de un job de migración:
        /// - JSON
        /// - Markdown
        /// - Excel
        /// 
        /// También muestra un resumen en consola del estado de los pasos y del job completo.
        /// </summary>
        /// <param name="job">Job de migración ejecutado.</param>
        /// <param name="logs">Lista de logs de cada paso.</param>
        /// <param name="reportDto">DTO listo para Excel.</param>
        /// <param name="excelPath">Ruta completa del archivo Excel.</param>
        /// <param name="jsonWriter">Escritor para logs JSON.</param>
        /// <param name="mdWriter">Escritor para logs Markdown.</param>
        /// <param name="reportWriter">Escritor para generar reportes Excel.</param>
        public static void EscribirReportes(
            MigrationJob job,
            List<LogEntry> logs,
            MigrationJobReportDto reportDto,
            string excelPath,
            ILogWriterJSON jsonWriter,
            ILogWriterMD mdWriter,
            MigrationReportExcelWriter reportWriter)
        {
            // Escribir logs en JSON
            jsonWriter.EscribirLog(job.Nombre, logs);

            // Escribir logs en Markdown TODO: Habilitar generación de MD más adelante
            //mdWriter.EscribirLog(job.Nombre, logs);

            // Mostrar resumen en consola
            MostrarResumen(job, logs);

            // Generar Excel
            reportWriter.WriteJobReport(reportDto, excelPath);
            Console.WriteLine($"Reporte Excel generado: {excelPath}");
        }

        /// <summary>
        /// Muestra en consola un resumen del job:
        /// - Total de pasos
        /// - Éxitos y fallidos
        /// - Estado final del job
        /// </summary>
        /// <param name="job">Job de migración ejecutado.</param>
        /// <param name="logs">Logs de cada paso del job.</param>
        private static void MostrarResumen(MigrationJob job, List<LogEntry> logs)
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