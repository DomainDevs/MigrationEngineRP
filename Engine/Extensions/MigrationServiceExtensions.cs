using Core.Entities;
using Engine.Hubs;
using Infrastructure.DTOs;
using Microsoft.AspNetCore.SignalR;
//using MigrationExecutor.WebAPI.Hubs;

namespace Engine.Extensions
{
    /// <summary>
    /// Extensiones para la clase MigrationService. Métodos estáticos y modulares
    /// Esta clase contiene métodos auxiliares para:
    /// 1. Inicializar logs de pasos de migración.
    /// 2. Ejecutar los pasos de un job de forma secuencial, manejando errores.
    /// 3. Crear un MigrationJoba partir de una carpeta que contiene paquetes SSIS (*.dtsx).
    /// </summary>
    public static class MigrationServiceExtensions
    {
        /// Inicializa la lista de logs para cada paso de un job de migración. 
        /// Asigna el nombre del archivo Excel
        public static List<LogEntry> InicializarLogs(MigrationJob job, string excelFileName)
        {
            return job.Pasos.Select(step => new LogEntry
            {
                NombrePaso = step.Nombre,
                Inicio = step.Inicio,    // Fecha inicial (puede ser null)
                FileXLS = Path.GetFileName(excelFileName) // Solo nombre del archivo
            }).ToList();
        }

        /// Ejecuta todos los pasos de un MigrationJob
        /// Cada paso se registra en su correspondiente -> LogEntry
        public static List<LogEntry> EjecutarPasos(MigrationJob job, List<LogEntry> logs,
            IHubContext<MigrationHub> hubContext //Hub
            )
        {
            for (int i = 0; i < job.Pasos.Count; i++)
            {
                var step = job.Pasos[i];
                logs[i] = EjecutarPaso(step, logs[i]);

                var porcentaje = (i + 1) * 100 / job.Pasos.Count;
                // Enviar al Hub
                hubContext.Clients.All.SendAsync("RecibirProgreso", job.Id, porcentaje, step.Nombre);

            }
            return logs;
        }

        /// Ejecuta un solo paso de migración.
        /// Maneja la lógica de éxito/fallo, captura excepciones, registra tiempos y mensajes.
        private static LogEntry EjecutarPaso(MigrationStep step, LogEntry logEntry)
        {
            step.Inicio = DateTime.Now;
            logEntry.Inicio = step.Inicio;

            try
            {
                // Aquí se ejecutaría el paquete SSIS o la lógica del paso real
                step.Exito = true;
                step.Mensaje = "Paso ejecutado correctamente";
                logEntry.Exito = true;
                logEntry.Mensaje = step.Mensaje;

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"> {step.Nombre} [OK]");
            }
            catch (Exception ex)
            {
                // Captura fallos y registra mensaje
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
                logEntry.Fin = step.Fin; //fecha de fin del paso
            }

            return logEntry;
        }

        /// Crea un MigrationJob a partir de una carpeta que contiene paquetes SSIS (*.dtsx).
        /// Permite incluir u omitir paquetes específicos según listas proporcionadas en el JSON.
        public static MigrationJob CrearJobDesdeCarpeta(
            string nombreJob,
            string carpetaPaquetes,
            List<string>? paquetesIncluir = null,
            List<string>? paquetesOmitir = null)
        {
            if (string.IsNullOrWhiteSpace(nombreJob)) throw new ArgumentException("Nombre del job requerido.", nameof(nombreJob));
            if (!Directory.Exists(carpetaPaquetes)) throw new DirectoryNotFoundException($"La carpeta {carpetaPaquetes} no existe.");

            var archivosDisponibles = Directory.GetFiles(carpetaPaquetes, "*.dtsx");
            var nombresDisponibles = archivosDisponibles.Select(Path.GetFileName).ToList();

            // Validación de paquetes a omitir
            if (paquetesOmitir != null)
            {
                var omitidosNoEncontrados = paquetesOmitir
                    .Where(p => !nombresDisponibles.Contains(p, StringComparer.OrdinalIgnoreCase))
                    .ToList();

                if (omitidosNoEncontrados.Any())
                    throw new FileNotFoundException($"Paquetes a omitir no encontrados: {string.Join(", ", omitidosNoEncontrados)}");
            }

            // Validación de paquetes a incluir
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

            // Excluir paquetes indicados
            if (paquetesOmitir != null && paquetesOmitir.Count > 0)
                archivosDisponibles = archivosDisponibles
                    .Where(f => !paquetesOmitir.Contains(Path.GetFileName(f), StringComparer.OrdinalIgnoreCase))
                    .ToArray();

            if (archivosDisponibles.Length == 0)
                throw new InvalidOperationException("No hay paquetes válidos para ejecutar.");

            // Crear pasos del job
            var pasos = archivosDisponibles.Select(a => new MigrationStep
            {
                Nombre = Path.GetFileNameWithoutExtension(a),
                RutaPaquete = a
            }).ToList();

            return new MigrationJob
            {
                Nombre = nombreJob,
                Pasos = pasos,
                FechaEjecucion = DateTime.Now
            };
        }
    }
}