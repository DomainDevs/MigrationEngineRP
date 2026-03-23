using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Infrastructure.Logging
{
    public class LogWriterMD : ILogWriterMD
    {
        private readonly string _carpetaBase;

        public LogWriterMD(string carpetaBase)
        {
            if (string.IsNullOrWhiteSpace(carpetaBase))
                throw new ArgumentException("Debe indicar una carpeta base para los logs.", nameof(carpetaBase));

            _carpetaBase = carpetaBase;

            if (!Directory.Exists(_carpetaBase))
                Directory.CreateDirectory(_carpetaBase);
        }

        /// <summary>
        /// Escribe el log en un archivo Markdown (.md)
        /// </summary>
        /// <param name="nombreArchivo">Nombre del archivo sin extensión</param>
        /// <param name="entradas">Lista de logs a escribir</param>
        public void EscribirLog(string nombreArchivo, List<LogEntry> entradas)
        {
            if (entradas == null || entradas.Count == 0)
                return; // No hay nada que escribir

            string archivo = Path.Combine(_carpetaBase, $"{nombreArchivo}_{DateTime.Now:yyyyMMdd_HHmmss}.md");

            var sb = new StringBuilder();

            sb.AppendLine($"# Reporte de Ejecución - {nombreArchivo}");
            sb.AppendLine();
            sb.AppendLine($"Generado: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();

            int exitos = 0;
            int fallidos = 0;

            foreach (var log in entradas)
            {
                sb.AppendLine($"## Paso: {log.NombrePaso}");
                sb.AppendLine("```text");
                // Mostrar inicio y fin con milisegundos
                sb.AppendLine($"Inicio: {log.Inicio:HH:mm:ss.fff}");
                sb.AppendLine($"Fin:    {log.Fin:HH:mm:ss.fff}");
                sb.AppendLine($"Duración: {(log.Fin - log.Inicio).TotalSeconds:F2} segundos");
                sb.AppendLine($"Resultado: {(log.Exito ? "Success" : "Failure")}");
                if (!string.IsNullOrWhiteSpace(log.Mensaje))
                    sb.AppendLine($"Mensaje: {log.Mensaje}");
                sb.AppendLine("```");
                sb.AppendLine();

                if (log.Exito) exitos++;
                else fallidos++;
            }

            // Resumen general
            sb.AppendLine("# Resumen de Ejecución");
            sb.AppendLine("```text");
            sb.AppendLine($"Total pasos: {entradas.Count}");
            sb.AppendLine($"Éxitos:      {exitos}");
            sb.AppendLine($"Fallidos:    {fallidos}");
            sb.AppendLine("```");

            File.WriteAllText(archivo, sb.ToString());
        }
    }
}