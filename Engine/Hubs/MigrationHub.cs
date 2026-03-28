using Microsoft.AspNetCore.SignalR;

namespace Engine.Hubs;

public class MigrationHub : Hub
{
    public async Task BroadcastProgress(Guid jobId, int porcentaje, string nombrePaso)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"[Hub] BroadcastProgress recibido: {porcentaje}% - {nombrePaso}");
        Console.ResetColor();
        await Clients.All.SendAsync("RecibirProgreso", jobId, porcentaje, nombrePaso);
    }
}