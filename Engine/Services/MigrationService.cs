using Core.Entities;
using Infrastructure.Logging;

namespace Engine.Services
{
    public class MigrationService
    {
        private readonly ILogWriterMD _mdWriter;
        private readonly ILogWriterJSON _jsonWriter;

        // Evento público que notificará progreso
        public event EventHandler<MigrationProgressEventArgs>? OnProgressChanged;

        // ⚡ Estado global
        private static int _isRunning = 0; // 0 = libre, 1 = en ejecución
        private static readonly object _lock = new object();
        private static MigrationJob? _currentJob; // Job en ejecución, accesible a otros clientes

        public MigrationService(ILogWriterMD mdWriter, ILogWriterJSON jsonWriter)
        {
            _mdWriter = mdWriter ?? throw new ArgumentNullException(nameof(mdWriter));
            _jsonWriter = jsonWriter ?? throw new ArgumentNullException(nameof(jsonWriter));
        }

        private LogEntry EjecutarPaso(MigrationStep step)
        {
            step.Inicio = DateTime.Now;
            var logEntry = new LogEntry { NombrePaso = step.Nombre, Inicio = step.Inicio };

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

        public bool EjecutarJob(MigrationJob job)
        {
            if (job == null) throw new ArgumentNullException(nameof(job));
            if (job.Pasos == null || job.Pasos.Count == 0) return false;

            lock (_lock)
            {
                if (Interlocked.CompareExchange(ref _isRunning, 1, 0) != 0)
                {
                    // Ya hay un job en ejecución
                    return false;
                }
                _currentJob = job;
            }

            try
            {
                job.FechaEjecucion = DateTime.Now;
                var logs = new List<LogEntry>();
                int totalSteps = job.Pasos.Count;

                for (int i = 0; i < totalSteps; i++)
                {
                    var step = job.Pasos[i];
                    var log = EjecutarPaso(step);
                    logs.Add(log);

                    // Disparar evento de progreso
                    OnProgressChanged?.Invoke(this, new MigrationProgressEventArgs
                    {
                        Progress = ((i + 1) * 100.0) / totalSteps,
                        StepName = step.Nombre,
                        Job = job
                    });
                }

                job.Completado = job.Pasos.All(p => p.Exito);

                // Generar logs
                _mdWriter.EscribirLog(job.Nombre, logs);
                _jsonWriter.EscribirLog(job.Nombre, logs);

                // Mostrar resumen final
                MostrarResumen(job, logs);

                return true;
            }
            finally
            {
                lock (_lock)
                {
                    _currentJob = null;
                    Interlocked.Exchange(ref _isRunning, 0); // liberar ejecución
                }
            }
        }

        // Permite consultar el job actual desde otro cliente
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

            // Validar paquetesOmitir
            if (paquetesOmitir != null && paquetesOmitir.Count > 0)
            {
                var omitidosNoEncontrados = paquetesOmitir
                    .Where(p => !nombresDisponibles.Contains(p, StringComparer.OrdinalIgnoreCase))
                    .ToList();

                if (omitidosNoEncontrados.Any())
                    throw new FileNotFoundException($"Paquetes a omitir no encontrados: {string.Join(", ", omitidosNoEncontrados)}");
            }

            // Filtrar paquetesIncluir
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

            // Excluir paquetesOmitir
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

    // Argumentos de progreso
    public class MigrationProgressEventArgs : EventArgs
    {
        public double Progress { get; set; }
        public string StepName { get; set; } = string.Empty;
        public MigrationJob? Job { get; set; }
    }
}