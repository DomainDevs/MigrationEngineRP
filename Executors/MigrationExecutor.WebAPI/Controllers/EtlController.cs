using Engine.Interface;
using Microsoft.AspNetCore.Mvc;


namespace MigrationExecutor.WebAPI.Controllers;

[ApiController]
[Route("api/etl")]
public class EtlController : ControllerBase
{
    private readonly IEtlPackageProvider _provider;

    public EtlController(IEtlPackageProvider provider)
    {
        _provider = provider;
    }

    [HttpGet("files")]
    public IActionResult GetEtlFiles()
    {
        return Ok(_provider.GetPackages());
    }
}