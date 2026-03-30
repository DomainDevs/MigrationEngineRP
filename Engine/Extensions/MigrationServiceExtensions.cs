using Core.Entities;
using Engine.Hubs;
using Infrastructure.Config;
using Infrastructure.DTOs;
using Microsoft.AspNetCore.SignalR;

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
            IHubContext<MigrationHub> hubContext, MigrationConfig config
            )
        {
            for (int i = 0; i < job.Pasos.Count; i++)
            {
                var step = job.Pasos[i];
                logs[i] = EjecutarPaso(step, logs[i], config);

                var porcentaje = (i + 1) * 100 / job.Pasos.Count;
                // Enviar al Hub
                //Console.ForegroundColor = ConsoleColor.Cyan;
                //Console.WriteLine($"[Hub] Enviando progreso: {porcentaje}% - {step.Nombre}");
                //Console.ResetColor();

                //hubContext.Clients.All.SendAsync("RecibirProgreso", job.Id, porcentaje, step.Nombre);
                //Cambie a fire & forget, porque no lo soporta
                _ = hubContext.Clients.All.SendAsync(
                    "RecibirProgreso",
                    job.Id,
                    porcentaje,
                    step.Nombre
                );

            }
            return logs;
        }

        /// Ejecuta un solo paso de migración.
        /// Maneja la lógica de éxito/fallo, captura excepciones, registra tiempos y mensajes.
        /*
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
        */
        private static LogEntry EjecutarPaso(MigrationStep step, LogEntry logEntry, MigrationConfig config)
        {
            step.Inicio = DateTime.Now;
            logEntry.Inicio = step.Inicio;

            try
            {
                // === Aquí se ejecutaría el paquete SSIS real ===
                // Por ejemplo, usando DTExec u otra API interna
                // step.RutaPaquete tiene la ruta del .dtsx a ejecutar
                EjecutarPaqueteETL(step.RutaPaquete, config); // <-- tu función real de ejecución ETL

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
                step.Fin = DateTime.Now;
                logEntry.Fin = step.Fin;
                Console.ResetColor();
            }

            return logEntry;
        }
        /*
        private static void EjecutarPaqueteETL(string rutaPaquete, MigrationConfig config)
        {
            if (string.IsNullOrWhiteSpace(rutaPaquete) || !File.Exists(rutaPaquete))
                throw new FileNotFoundException($"No se encontró el paquete: {rutaPaquete}");

            var proceso = new System.Diagnostics.Process();

            proceso.StartInfo.FileName = "dtexec.exe"; // Ejecutable de SSIS

            // --- Crear archivos .conmgr temporales ---
            string tempSourceConMgr = Path.Combine(Path.GetTempPath(), $"BDSource_{Guid.NewGuid()}.conmgr");
            string tempDestConMgr = Path.Combine(Path.GetTempPath(), $"DestinationDB_{Guid.NewGuid()}.conmgr");

            File.Copy(Path.Combine(Path.GetDirectoryName(rutaPaquete)!, "SourceDB.conmgr"), tempSourceConMgr, true);
            File.Copy(Path.Combine(Path.GetDirectoryName(rutaPaquete)!, "DestinationDB.conmgr"), tempDestConMgr, true);

            // Reemplazar cadenas de conexión dentro de los .conmgr
            File.WriteAllText(tempSourceConMgr,
            File.ReadAllText(tempSourceConMgr).Replace("Data Source=.*?;", config.SourceDB)); // Ajusta patrón si es necesario
            File.WriteAllText(tempDestConMgr,
            File.ReadAllText(tempDestConMgr).Replace("Data Source=.*?;", config.DestinationDB));

            // --- Ejecutar el paquete con DTExec ---
            proceso.StartInfo.Arguments =
                $"/F \"{rutaPaquete}\" " +
                $"/ConnMgrFile \"{tempSourceConMgr}\" " +
                $"/ConnMgrFile \"{tempDestConMgr}\"";

            //proceso.StartInfo.Arguments = $"/F \"{rutaPaquete}\""; // Ruta del paquete

            proceso.StartInfo.UseShellExecute = false;
            proceso.StartInfo.RedirectStandardOutput = true;
            proceso.StartInfo.RedirectStandardError = true;
            proceso.StartInfo.CreateNoWindow = true;

            proceso.Start();

            string output = proceso.StandardOutput.ReadToEnd();
            string error = proceso.StandardError.ReadToEnd();

            proceso.WaitForExit();

            if (proceso.ExitCode != 0)
                throw new Exception($"Error al ejecutar paquete: {error}\n{output}");
        }*/
        private static void EjecutarPaqueteETL(string rutaPaquete, MigrationConfig config)
        {
            if (string.IsNullOrWhiteSpace(rutaPaquete) || !File.Exists(rutaPaquete))
                throw new FileNotFoundException($"No se encontró el paquete: {rutaPaquete}");

            string carpetaPaquete = Path.GetDirectoryName(rutaPaquete)!;

            // ======== Buscar archivos que contengan "Source" o "Destination" ========
            /*
            var sourceFiles = Directory.GetFiles(carpetaPaquete, "*Source*.conmgr");
            var destFiles = Directory.GetFiles(carpetaPaquete, "*Destination*.conmgr");

            if (sourceFiles.Length == 0)
                throw new FileNotFoundException($"No se encontró ningún Connection Manager de origen en {carpetaPaquete}");

            if (destFiles.Length == 0)
                throw new FileNotFoundException($"No se encontró ningún Connection Manager de destino en {carpetaPaquete}");
            

            // Si hay varios, puedes tomar el primero o lanzar error según tu política
            string sourceConMgrOriginal = sourceFiles[0];
            string destConMgrOriginal = destFiles[0];
            */

            var proceso = new System.Diagnostics.Process();
            proceso.StartInfo.FileName = @"C:\Program Files\Microsoft SQL Server\150\DTS\Binn\DTExec.exe";
            //proceso.StartInfo.FileName = "dtexec.exe";
            proceso.StartInfo.Arguments = $"/F \"{rutaPaquete}\"";
            proceso.StartInfo.UseShellExecute = false;
            proceso.StartInfo.RedirectStandardOutput = true;
            proceso.StartInfo.RedirectStandardError = true;
            proceso.StartInfo.CreateNoWindow = true;
            proceso.Start();

            /*
            proceso.StartInfo.Arguments =
                $"/F \"{rutaPaquete}\" " +
                $"/SET \\Package.Connections[SourceDB].ConnectionString;\"{config.SourceDB}\" " +
                $"/SET \\Package.Connections[DestinationDB].ConnectionString;\"{config.DestinationDB}\"";
            */

            /*
            string EscapeForDTExec(string conn)
            {
                return conn.Replace("\"", "'"); // Reemplaza dobles por simples
            }
            proceso.StartInfo.Arguments =
                $"/F \"{rutaPaquete}\" " +
                $"/SET \\Package.Connections[SourceDB].ConnectionString;\"{EscapeForDTExec(config.SourceDB)}\" " +
                $"/SET \\Package.Connections[DestinationDB].ConnectionString;\"{EscapeForDTExec(config.DestinationDB)}\"";
            */
            /*
            string EscapeForDTExec(string conn) => conn.Replace("\"", "'");

            proceso.StartInfo.Arguments =
                $"/F \"{rutaPaquete}\" " +
                $"/SET \\Package.Connections[SourceDB].ConnectionString;\"{EscapeForDTExec(config.SourceDB)}\" " +
                $"/SET \\Package.Connections[DestinationDB].ConnectionString;\"{EscapeForDTExec(config.DestinationDB)}\"";
            */

            /*
            string EscapeForDTExec(string conn) => conn.Replace("\"", "'");

            proceso.StartInfo.Arguments =
                "/F \"" + rutaPaquete + "\" " +
                "/SET \\Package.Connections[SourceDB].ConnectionString;\"" + EscapeForDTExec(config.SourceDB) + "\" " +
                "/SET \\Package.Connections[DestinationDB].ConnectionString;\"" + EscapeForDTExec(config.DestinationDB) + "\"";
            */
            /*
            string EscapeForDTExec(string conn)
            {
                return conn.Replace("\"", "\\\""); // Escapa comillas dobles
            }

            proceso.StartInfo.Arguments =
                $"/F \"{rutaPaquete}\" " +
                $"/SET \\Package.Connections[SourceDB].Properties[ConnectionString];\"{EscapeForDTExec(config.SourceDB)}\" " +
                $"/SET \\Package.Connections[DestinationDB].Properties[ConnectionString];\"{EscapeForDTExec(config.DestinationDB)}\"";


            proceso.StartInfo.Arguments = $"/F \"{rutaPaquete}\"";
            */

            string output = proceso.StandardOutput.ReadToEnd();
            string error = proceso.StandardError.ReadToEnd();

            proceso.WaitForExit();


            if (proceso.ExitCode != 0)
                throw new Exception($"Error al ejecutar paquete: {error}\n{output}");
        }




        /// Crea un MigrationJob a partir de una carpeta que contiene paquetes SSIS (*.dtsx).
        /// Permite incluir u omitir paquetes específicos según listas proporcionadas en el JSON.
        public static MigrationJob CrearJobDesdeCarpeta(
            string nombreJob,
            string carpetaPaquetes,
            string SourceDB,
            string DestinationDB,
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