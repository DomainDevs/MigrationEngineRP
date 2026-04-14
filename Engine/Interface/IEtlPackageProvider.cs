using Core.Entities;

namespace Engine.Interface;

public interface IEtlPackageProvider
{
    IEnumerable<EtlPackageInfo> GetPackages();
}