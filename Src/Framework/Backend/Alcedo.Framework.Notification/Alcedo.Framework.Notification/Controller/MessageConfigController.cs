using Alcedo.Core.Presentation;
using Alcedo.Framework.Notification.Application.Command;
using Alcedo.Framework.Notification.Application.Dto;
using Alcedo.Framework.Notification.Application.SearchQuery;
using Alcedo.Framework.Notification.Application.Service.MessageConfig;
using Alcedo.Framework.Notification.Infrastructure.Email;
using Alcedo.Framework.Shared.Controller;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace Alcedo.Framework.Notification.Controller;

[Area("notification")]
[ApiVersion("1.0")]
[Resource("MessageConfigs", "Manage Notification Configs")]
public sealed class MessageConfigsController : MasterDataControllerBase<MessageConfigDto, MessageConfigSearchQuery
    , MessageConfigCreateCommand, MessageConfigUpdateCommand>
{
    private readonly IMessageConfigApplicationService _service;
    private readonly EmailService _emailService;

    public MessageConfigsController(IMessageConfigApplicationService service, EmailService emailService) : base(service)
    {
        _service = service;
        _emailService = emailService;
    }
    
    [HttpPatch("{id}/skip:true")]
    [Operation("Skip", "Set config be ignored when being sent")]
    public async Task<IActionResult> SkipConfig(Guid id, CancellationToken cancellationToken = default)
    {
        await _service.SetSkipByIdAsync(id, CurrentUser, cancellationToken);
        return Ok();
    }
    
    [HttpPatch("{id}/skip:false")]
    [Operation("UnSkip", "Set config unskippable")]
    public async Task<IActionResult> UnSkipConfig(Guid id, CancellationToken cancellationToken = default)
    {
        await _service.SetUnSkipByIdAsync(id, CurrentUser, cancellationToken);
        return Ok();
    }

    [HttpPatch("{id}/templates:assign")]
    [Operation("AssignMessageTemplates", "Batch Assign MessageTemplates to Config")]
    public async Task<IActionResult> AssignMessageTemplates(Guid id, [FromBody] IReadOnlyCollection<Guid> templateIds,
        CancellationToken cancellationToken = default)
    {
        await _service.BatchAssignMessageTemplatesAsync(id, templateIds, CurrentUser, cancellationToken);
        return Ok();
    }

    [HttpPatch("{id}/templates:delete")]
    [Operation("DeleteMessageTemplates", "Batch Delete MessageTemplates Of A Config")]
    public async Task<IActionResult> DeleteRole(Guid id, [FromBody] IReadOnlyCollection<Guid> templateIds,
        CancellationToken cancellationToken = default)
    {
        await _service.BatchRemoveMessageTemplatesAsync(id, templateIds, CurrentUser, cancellationToken);
        return Ok();
    }

    [HttpPatch("{id}/templates:clear")]
    [Operation("ClearMessageTemplates", "Clear MessageTemplates For A Config")]
    public async Task<IActionResult> ClearMessageTemplates(Guid id, CancellationToken cancellationToken = default)
    {
        await _service.ClearMessageTemplatesAsync(id, CurrentUser, cancellationToken);
        return Ok();
    }

    [HttpPost("{id}/test")]
    [Operation("Test Config",
        "For Testing notification Send; will send blank notification to the operator for testing purpose")]
    public async Task<ActionResult<IReadOnlyList<MessageTemplateDto>>> TestMessageConfigAsync(
        Guid id, [FromBody] string jsonPayload,
        CancellationToken cancellationToken = default)
    {
        await _service.TestMessageConfigAsync(id, jsonPayload, CurrentUser, cancellationToken);
        return Ok();
    }

    [HttpGet("{id}/templates")]
    [Operation("View Templates", "View Templates Of a Message Config")]
    public async Task<ActionResult<IReadOnlyList<MessageTemplateDto>>> GetMessageTemplateDtosByMessageConfigIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var roles = await _service.GetMessageTemplateDtosByMessageConfigIdAsync(id, cancellationToken);
        return Ok(roles);
    }
}