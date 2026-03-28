namespace MigrationExecutor.WebAPI.Utils;

public class MigrationConfig
{
    public string NombreJob { get; set; } = string.Empty;
    public string CarpetaPaquetes { get; set; } = string.Empty;
    public string CarpetaLogs { get; set; } = string.Empty;
    public string SourceDB { get; set; } = string.Empty;
    public string DestinationDB { get; set; } = string.Empty;
    public List<string>? PaquetesIncluir { get; set; } = new();
    public List<string>? PaquetesOmitir { get; set; } = new();

}