using Core.Entities;
using Engine.Interface;
using Infrastructure.Config;

namespace Engine.Services;

public class FileSystemEtlPackageProvider : IEtlPackageProvider
{
    private readonly MigrationConfig _config;

    public FileSystemEtlPackageProvider(MigrationConfig config)
    {
        _config = config;
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