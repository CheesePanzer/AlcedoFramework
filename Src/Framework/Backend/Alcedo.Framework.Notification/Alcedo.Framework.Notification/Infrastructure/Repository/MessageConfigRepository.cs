using Alcedo.Core.Abstractions;
using Alcedo.Core.Infrastructure.EfCore;
using Alcedo.Framework.Notification.Domain.Entity;
using Alcedo.Framework.Notification.Domain.IRepository;
using Alcedo.Framework.Shared.Infrastructure.Repository;
using Microsoft.EntityFrameworkCore;

namespace Alcedo.Framework.Notification.Infrastructure.Repository;

[RegisterAs(typeof(IMessageConfigRepository))]
public class MessageConfigRepository : MasterDataRepositoryBase<MessageConfigAggRoot>, IMessageConfigRepository
{
    public MessageConfigRepository(AlcedoDbContext context) : base(context)
    {
    }

    public Task<MessageConfigAggRoot?> GetByMsgTypeCodeNoTrackingAsync(string msgTypeCode, CancellationToken ct = default)
        => DbSet.Where(cfg => cfg.MessageTypeCode == msgTypeCode).AsNoTracking().SingleOrDefaultAsync(cancellationToken: ct);
}