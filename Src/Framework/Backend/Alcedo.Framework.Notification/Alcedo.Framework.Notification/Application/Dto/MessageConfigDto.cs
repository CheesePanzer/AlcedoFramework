using Alcedo.Framework.Shared.Application.Dto;

namespace Alcedo.Framework.Notification.Application.Dto;

public sealed record MessageConfigDto : MasterDataDtoBase
{
    public required string MessageTypeCode { get; init; }
    
    public required bool ShouldSkip { get; init; }

    public required IReadOnlyCollection<Guid> TemplateIds { get; init; }
}