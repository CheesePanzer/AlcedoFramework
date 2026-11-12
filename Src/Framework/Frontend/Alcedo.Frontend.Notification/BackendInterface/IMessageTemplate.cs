using Alcedo.Frontend.Abstraction;
using Alcedo.Frontend.Abstraction.Attributes;
using Alcedo.Frontend.Abstraction.BackednInterface;
using Alcedo.Frontend.Abstraction.Command;
using Alcedo.Frontend.Abstraction.Dto;
using Alcedo.Frontend.Abstraction.SearchQuery;

namespace Alcedo.Frontend.Notification.BackendInterface;

public enum NotificationChannel
{
    Email,
    Sms,
    InApp
}

[AutoTableLink(nameof(Code), "/notification/templates/{Id}")]
public sealed record MessageTemplateDto : MasterDataDtoBase
{
    public NotificationChannel Channel { get; set; }
    
    public string? TitleTemplate { get; set; }
    
    [AutoTableIgnoreRender]
    public string? BodyTemplate { get; set; }

    public string? ExternalTemplateId { get; set; }
}

public sealed record MessageTemplateCreateCommand : MasterDataCreateCommandBase
{
    public NotificationChannel Channel { get; set; }
    
    public string? ExternalTemplateId { get; set; }
    
    [AutoFormRenderTextArea(lines:3, maxLines:12)]
    [AutoFormCellWidth(12)]
    public string? TitleTemplate { get; set; }
    
    [AutoFormRenderTextArea(lines:6, maxLines:12)]
    [AutoFormCellWidth(12)]
    public string? BodyTemplate { get; set; }
}

public sealed record MessageTemplateUpdateCommand : MasterDataUpdateCommandBase
{
    public NotificationChannel Channel { get; set; }
    
    public string? ExternalTemplateId { get; set; }
    
    [AutoFormRenderTextArea(lines:3, maxLines:12)]
    [AutoFormCellWidth(12)]
    public string? TitleTemplate { get; set; }
    
    [AutoFormRenderTextArea(lines:6, maxLines:12)]
    [AutoFormCellWidth(12)]
    public string? BodyTemplate { get; set; }
}

public sealed record MessageTemplateTestRenderResult
{
    public string Title { get; set; } = null!;

    public string Body { get; set; } = null!;
}

public sealed record MessageTemplateSearchQuery : MasterDataSearchQueryBase;

[ApiClient(Area = "notification", Controller = "messagetemplates", Version = 1)]
public interface IMessageTemplate : IMasterDataController<MessageTemplateDto, MessageTemplateSearchQuery,
    MessageTemplateCreateCommand, MessageTemplateUpdateCommand>
{
    [HttpPost("{id}/test")]
    Task<MessageTemplateTestRenderResult> TestRenderAsync([ToRoute] Guid id, [ToBody] string payload,
        CancellationToken ct = default);
}