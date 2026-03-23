using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Entities;

public class MigrationStep
{
    public string Nombre { get; set; }
    public string RutaPaquete { get; set; } // La ruta al .dtsx
    public bool Exito { get; set; }
    public DateTime Inicio { get; set; }
    public DateTime Fin { get; set; }
    public string Mensaje { get; set; }
}
