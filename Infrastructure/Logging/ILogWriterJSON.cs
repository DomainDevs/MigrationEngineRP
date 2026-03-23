
namespace Infrastructure.Logging
{
    public interface ILogWriterJSON
    {
        void EscribirLog(string nombreArchivo, List<LogEntry> entradas);
    }
}