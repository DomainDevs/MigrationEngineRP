using Core.Entities;
using Infrastructure.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Engine.Services
{
    public class MigrationService
    {
        private readonly ILogWriterMD _mdWriter;
        private readonly ILogWriterJSON _jsonWriter;

        public MigrationService(ILogWriterMD mdWriter, ILogWriterJSON jsonWriter)
        {
            _mdWriter = mdWriter ?? throw new ArgumentNullException(nameof(mdWriter));
            _jsonWriter = jsonWriter ?? throw new ArgumentNullException(nameof(jsonWriter));
        }

        private LogEntry EjecutarPaso(MigrationStep step)
        {
            step.Inicio = DateTime.Now;
            var logEntry = new LogEntry
            {
                NombrePaso = step.Nombre,
                Inicio = step.Inicio
            };

            try
            {
                // Aquí se ejecutaría el paquete SSIS
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

        public void EjecutarJob(MigrationJob job)
        {
            if (job == null) throw new ArgumentNullException(nameof(job));
            if (job.Pasos == null || job.Pasos.Count == 0) return;

            job.FechaEjecucion = DateTime.Now;

            var logs = job.Pasos.Select(EjecutarPaso).ToList();
            job.Completado = job.Pasos.All(p => p.Exito);

            // Generar ambos logs
            _mdWriter.EscribirLog(job.Nombre, logs);
            _jsonWriter.EscribirLog(job.Nombre, logs);

            // Mostrar resumen final
            MostrarResumen(job, logs);
        }

        public void EjecutarJobDesdeCarpeta(
            string nombreJob,
            string carpetaPaquetes,
            List<string>? paquetesIncluir = null,
            List<string>? paquetesOmitir = null)
        {
            if (string.IsNullOrWhiteSpace(nombreJob))
                throw new ArgumentException("Nombre del job requerido.", nameof(nombreJob));

            if (!Directory.Exists(carpetaPaquetes))
                throw new DirectoryNotFoundException($"La carpeta {carpetaPaquetes} no existe.");

            var archivosDisponibles = Directory.GetFiles(carpetaPaquetes, "*.dtsx");
            var nombresDisponibles = archivosDisponibles.Select(f => Path.GetFileName(f)).ToList();

            // Validar paquetesOmitir
            if (paquetesOmitir != null && paquetesOmitir.Count > 0)
            {
                var omitidosNoEncontrados = paquetesOmitir
                    .Where(p => !nombresDisponibles.Contains(p, StringComparer.OrdinalIgnoreCase))
                    .ToList();

                if (omitidosNoEncontrados.Any())
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\n[ERROR] Algunos paquetes a omitir no existen:");
                    foreach (var p in omitidosNoEncontrados)
                        Console.WriteLine($" - {p}");
                    Console.ResetColor();

                    throw new FileNotFoundException(
                        $"Paquetes a omitir no encontrados: {string.Join(", ", omitidosNoEncontrados)}");
                }
            }

            // Filtrar paquetesIncluir
            if (paquetesIncluir != null && paquetesIncluir.Count > 0)
            {
                var paquetesNoEncontrados = paquetesIncluir
                    .Where(p => !nombresDisponibles.Contains(p, StringComparer.OrdinalIgnoreCase))
                    .ToList();

                if (paquetesNoEncontrados.Any())
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\n[ERROR] Algunos paquetes a incluir no existen:");
                    foreach (var p in paquetesNoEncontrados)
                        Console.WriteLine($" - {p}");
                    Console.ResetColor();

                    throw new FileNotFoundException(
                        $"Paquetes a incluir no encontrados: {string.Join(", ", paquetesNoEncontrados)}");
                }

                archivosDisponibles = archivosDisponibles
                    .Where(f => paquetesIncluir.Contains(Path.GetFileName(f), StringComparer.OrdinalIgnoreCase))
                    .ToArray();
            }

            // Excluir paquetesOmitir
            if (paquetesOmitir != null && paquetesOmitir.Count > 0)
            {
                archivosDisponibles = archivosDisponibles
                    .Where(f => !paquetesOmitir.Contains(Path.GetFileName(f), StringComparer.OrdinalIgnoreCase))
                    .ToArray();
            }

            if (archivosDisponibles.Length == 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\n[ERROR] No hay paquetes válidos para ejecutar.");
                Console.ResetColor();
                return;
            }

            var pasos = archivosDisponibles.Select(a => new MigrationStep
            {
                Nombre = Path.GetFileNameWithoutExtension(a),
                RutaPaquete = a
            }).ToList();

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
            Console.WriteLine($"Éxitos:      {exitos}");
            Console.WriteLine($"Fallidos:    {fallidos}");
            Console.ResetColor();

            Console.ForegroundColor = job.Completado ? ConsoleColor.Green : ConsoleColor.Red;
            Console.WriteLine($"\nEstado final del Job: {(job.Completado ? "[OK]" : "[FAIL]")}");
            Console.ResetColor();
        }
    }
}