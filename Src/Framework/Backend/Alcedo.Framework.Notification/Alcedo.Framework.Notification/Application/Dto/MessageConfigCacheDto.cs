using Alcedo.Core.Application.Dto;

namespace Alcedo.Framework.Notification.Application.Dto;

public sealed record MessageConfigCacheDto : EntityCacheDtoBase
{
    public required bool IsActive { get; init; }
    public required string MessageTypeCode { get; init; }
    
    public required bool ShouldSkip { get; init; }

    public required IReadOnlyCollection<Guid> TemplateIds { get; init; }
}