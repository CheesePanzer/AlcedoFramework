using Alcedo.Core.Domain.Entity;

namespace Alcedo.Core.Domain.IStorageService;

public static class StorageObjectNameBuilder
{
    public static string GetObjectName(Type aggRootType, Guid id, string fileName)
    {
        var prefix = AggregateRootMetadataCenter.GetMetaData(aggRootType).Code;
        return $"{prefix}/{id}/{fileName}";
    }
    
    public static string GetObjectName<TAggRoot>(Guid id, string fileName)
    where TAggRoot : AggregateRoot
    {
        var prefix = AggregateRootMetadataCenter.GetMetaData<TAggRoot>().Code;
        return $"{prefix}/{id}/{fileName}";
    }

}