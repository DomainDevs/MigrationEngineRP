
using Infrastructure.Logging;
using Microsoft.AspNetCore.SignalR;
using MigrationExecutor.WebAPI.Hubs;

namespace MigrationExecutor.WebAPI.Hubs;

public class MigrationService
{
    private readonly ILogWriterMD _mdWriter;
    private readonly ILogWriterJSON _jsonWriter;
    private readonly IHubContext<MigrationHub> _hubContext; // <-- nuevo

    public MigrationService(
        ILogWriterMD mdWriter,
        ILogWriterJSON jsonWriter,
        IHubContext<MigrationHub> hubContext) // <-- inyección
    {
        _mdWriter = mdWriter ?? throw new ArgumentNullException(nameof(mdWriter));
        _jsonWriter = jsonWriter ?? throw new ArgumentNullException(nameof(jsonWriter));
        _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
    }
}