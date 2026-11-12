using Alcedo.Core.Domain.Entity;

namespace Alcedo.Framework.Shared.Domain.Entity;

public abstract class MasterDataArchivedEntityBase : EntityBase
{
    protected MasterDataArchivedEntityBase()
    {
        
    }
    
    protected MasterDataArchivedEntityBase(MasterDataAggregateRootBase originAgg, OperatorRecord archiver)
    {
        OriginId = originAgg.Id;
        OriginCode = originAgg.Code;
        OriginName = originAgg.Name;
        Archiver = archiver;
        ArchivedAt = DateTimeOffset.UtcNow;
    }
    
    public Guid OriginId { get; private set; }

    public string OriginCode { get; private set; }
    
    public string OriginName { get; private set; }
    
    public OperatorRecord Archiver { get; private set; }
    
    public DateTimeOffset ArchivedAt { get; private set; }
    
    
}