using Alcedo.Core.Abstractions;
using Alcedo.Core.Infrastructure.EfCore;
using Alcedo.Core.Infrastructure.QueryRepositoryBase;
using Alcedo.Framework.Notification.Application.Dto;
using Alcedo.Framework.Notification.Application.IQueryRepository;
using Alcedo.Framework.Notification.Application.SearchQuery;
using Alcedo.Framework.Notification.Domain.Entity;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace Alcedo.Framework.Notification.Infrastructure.QueryRepository;

[RegisterAs(typeof(IMessageConfigQueryRepository))]
internal sealed class MessageConfigQueryRepository :
    EntityQueryRepositoryBase<MessageConfigAggRoot, MessageConfigDto, MessageConfigSearchQuery>,
    IMessageConfigQueryRepository
{
    public MessageConfigQueryRepository(AlcedoDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<MessageTemplateDto>> GetMessageTemplateDtosByMessageConfigIdAsync(
        Guid messageConfigId, CancellationToken ct = default)
    {
        return await Context.AggRoot<MessageConfigAggRoot>()
            .AsNoTracking()
            .Where(u => u.Id == messageConfigId)
            .SelectMany(u => u.TemplatesRelations)
            .Join(Context.AggRoot<MessageTemplateAggRoot>(),
                ur => ur.MessageTemplateId,
                r => r.Id,
                (ur, r) => r)
            .ProjectToType<MessageTemplateDto>()
            .ToListAsync(ct);
    }
}