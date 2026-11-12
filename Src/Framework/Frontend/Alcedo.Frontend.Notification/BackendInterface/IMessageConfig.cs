using Alcedo.Frontend.Abstraction;
using Alcedo.Frontend.Abstraction.Attributes;
using Alcedo.Frontend.Abstraction.BackednInterface;
using Alcedo.Frontend.Abstraction.Command;
using Alcedo.Frontend.Abstraction.Dto;
using Alcedo.Frontend.Abstraction.SearchQuery;

namespace Alcedo.Frontend.Notification.BackendInterface;

[AutoTableLink(nameof(Code), "/notification/configs/{Id}")]
public sealed record MessageConfigDto : MasterDataDtoBase
{
    public string MessageTypeCode { get; set; } = null!;
    
    public bool ShouldSkip { get; set; }

    public IReadOnlyCollection<Guid> TemplateIds { get; set; } = null!;
}

public sealed record MessageConfigSearchQuery : MasterDataSearchQueryBase
{
}

public sealed record MessageConfigCreateCommand : MasterDataCreateCommandBase
{
    [AutoFormIgnoreRender] public string MessageTypeCode { get; set; } = null!;
}

public sealed record MessageConfigUpdateCommand : MasterDataUpdateCommandBase
{
    [AutoFormIgnoreRender] public string MessageTypeCode { get; set; } = null!;
}

[ApiClient(Area = "notification", Controller = "messageconfigs", Version = 1)]
public interface IMessageConfig : IMasterDataController<MessageConfigDto, MessageConfigSearchQuery,
    MessageConfigCreateCommand, MessageConfigUpdateCommand>
{
    [HttpPatch("{id}/skip:true")]
    Task SkipConfig(Guid id, CancellationToken cancellationToken = default);
    
    [HttpPatch("{id}/skip:false")]
    Task UnSkipConfig(Guid id, CancellationToken cancellationToken = default);
    
    [HttpPatch("{id}/templates:assign")]
    Task AssignMessageTemplatesAsync([ToRoute] Guid id, [ToBody] IReadOnlyCollection<Guid> templateIds,
        CancellationToken ct = default);

    [HttpPatch("{id}/templates:delete")]
    Task DeleteMessageTemplatesAsync([ToRoute] Guid id, [ToBody] IReadOnlyCollection<Guid> templateIds,
        CancellationToken ct = default);

    [HttpPatch("{id}/templates:clear")]
    Task ClearMessageTemplatesAsync([ToRoute] Guid id, CancellationToken ct = default);

    [HttpPost("{id}/test")]
    Task TestMessageConfigAsync([ToRoute] Guid id, [ToBody] string jsonPayload, CancellationToken ct = default);

    [HttpGet("{id}/templates")]
    Task<IReadOnlyList<MessageTemplateDto>> GetTemplatesByConfigIdAsync([ToRoute] Guid id,
        CancellationToken ct = default);
}