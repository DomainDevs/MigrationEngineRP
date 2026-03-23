using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.IO;

namespace MigrationExecutor.Console.Utils;

internal static class Startup
{
    private const string ConfigDirectory = "Configurations";

    internal static IHostBuilder AddConfigurations(this IHostBuilder host)
    {
        host.ConfigureAppConfiguration((hostingContext, config) =>
        {
            var env = hostingContext.HostingEnvironment;

            // ruta absoluta basada en el exe
            string basePath = AppContext.BaseDirectory;
            DirectoryInfo directory = new DirectoryInfo(Path.Combine(basePath, ConfigDirectory));

            if (directory.Exists)
            {
                foreach (FileInfo file in directory.EnumerateFiles("*.json"))
                {
                    string baseName = Path.GetFileNameWithoutExtension(file.Name);
                    string extension = Path.GetExtension(file.Name);

                    config.AddJsonFile(Path.Combine(directory.FullName, file.Name), optional: false, reloadOnChange: true);
                    config.AddJsonFile(Path.Combine(directory.FullName, $"{baseName}.{env.EnvironmentName}{extension}"), optional: true, reloadOnChange: true);
                }
            }

            config.AddEnvironmentVariables();
        });

        return host;
    }
}
