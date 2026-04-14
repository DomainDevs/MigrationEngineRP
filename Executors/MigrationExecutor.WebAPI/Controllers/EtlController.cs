using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/etl")]
public class EtlController : ControllerBase
{
    private readonly string _path = Path.Combine(Directory.GetCurrentDirectory(), "etl-packages");

    [HttpGet("files")]
    public IActionResult GetEtlFiles()
    {
        var files = Directory.GetFiles(_path, "*.dtsx")
            .Select(f => new
            {
                Name = Path.GetFileName(f),
                Path = f
            });

        return Ok(files);
    }
}