using Alcedo.Core.Application.IQueryRepository;
using Alcedo.Framework.Notification.Application.Dto;
using Alcedo.Framework.Notification.Application.SearchQuery;
using Alcedo.Framework.Notification.Domain.Entity;

namespace Alcedo.Framework.Notification.Application.IQueryRepository;

public interface
    IMessageConfigQueryRepository : IEntityQueryRepository<MessageConfigAggRoot, MessageConfigDto,
    MessageConfigSearchQuery>
{
    Task<IReadOnlyList<MessageTemplateDto>> GetMessageTemplateDtosByMessageConfigIdAsync(Guid messageConfigId,
        CancellationToken ct = default);
}