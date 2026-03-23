using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Entities;

public class MigrationJob
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Nombre { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.Now;
    public DateTime? FechaEjecucion { get; set; }
    public bool Completado { get; set; } = false;

    public List<MigrationStep> Pasos { get; set; } = new();
}
