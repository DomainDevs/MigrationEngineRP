using Microsoft.AspNetCore.SignalR;

namespace MigrationExecutor.WebAPI.Hubs
{
    public class MigrationHub : Hub
    {
        /// <summary>
        /// Envía el progreso de un paquete a todos los clientes conectados
        /// </summary>
        /// <param name="paquete">Nombre del paquete que se está ejecutando</param>
        /// <param name="completado">Número de pasos completados</param>
        /// <param name="total">Número total de pasos del paquete</param>
        public async Task SendProgress(string paquete, int completado, int total)
        {
            // Se envía un objeto JSON al cliente
            await Clients.All.SendAsync("ReceiveMessage", new { paquete, completado, total });
        }
    }
}