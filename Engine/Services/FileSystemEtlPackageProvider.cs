using Core.Entities;
using Engine.Interface;
using Infrastructure.Config;
using Microsoft.Extensions.Options;

namespace Engine.Services;

public class FileSystemEtlPackageProvider : IEtlPackageProvider
{
    private readonly MigrationConfig _config;

    public FileSystemEtlPackageProvider(IOptions<MigrationConfig> config)
    {
        _config = config.Value;
    }

    public IEnumerable<EtlPackageInfo> GetPackages()
    {
        return Directory.GetFiles(_config.CarpetaPaquetes, "*.dtsx")
            .Select(f => new EtlPackageInfo
            {
                Name = Path.GetFileName(f),
                FullPath = f
            });
    }
}