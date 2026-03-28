
using Infrastructure.DTOs;

namespace Infrastructure.Logging
{
    public interface ILogWriterMD
    {
        void EscribirLog(string nombreArchivo, List<LogEntry> entradas);
    }
}