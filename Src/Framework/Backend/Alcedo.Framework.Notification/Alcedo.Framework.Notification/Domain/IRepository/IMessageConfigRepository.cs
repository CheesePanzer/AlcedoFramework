using Alcedo.Framework.Notification.Domain.Entity;
using Alcedo.Framework.Shared.Domain.IRepositoryBase;

namespace Alcedo.Framework.Notification.Domain.IRepository;

public interface IMessageConfigRepository : IMasterDataRepositoryBase<MessageConfigAggRoot>
{
    Task<MessageConfigAggRoot?> GetByMsgTypeCodeNoTrackingAsync(string msgTypeCode, CancellationToken ct = default);
}