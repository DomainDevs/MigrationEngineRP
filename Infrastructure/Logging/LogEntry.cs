using System;

namespace Infrastructure.Logging
{
    public class LogEntry
    {
        public string NombrePaso { get; set; }
        public bool Exito { get; set; }
        public DateTime Inicio { get; set; }
        public DateTime Fin { get; set; }
        public string Mensaje { get; set; }
    }
}
