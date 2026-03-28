using Microsoft.AspNetCore.SignalR;

public class MigrationHub : Hub
{
    public async Task BroadcastProgress(Guid jobId, int porcentaje, string nombrePaso)
    {
        await Clients.All.SendAsync("RecibirProgreso", jobId, porcentaje, nombrePaso);
    }
}