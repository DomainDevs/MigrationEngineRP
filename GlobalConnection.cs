using System;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Sistran.MigrationEngine.MigracionSISE.UI
{
    // Clase estática para que todo el visor pueda usarla
    public static class GlobalConnections
    {
        // Diccionario de alias -> cadena de conexión
        public static Dictionary<string, string> Connections { get; private set; }

        // Inicializar desde JSON
        public static void Load(string path = null)
        {
            if (path == null)
            {
                path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "Connections.json");
            }

            if (!File.Exists(path))
                throw new FileNotFoundException($"No se encontró el archivo de conexiones: {path}");

            var jsonText = File.ReadAllText(path);
            Connections = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonText);
        }

        // Retorna solo los alias para mostrar en dropdown
        public static IEnumerable<string> GetAliases()
        {
            if (Connections == null) Load();
            return Connections.Keys;
        }
    }
}