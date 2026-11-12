using Alcedo.Core.Presentation;
using Alcedo.Framework.Notification.Application.Command;
using Alcedo.Framework.Notification.Application.Dto;
using Alcedo.Framework.Notification.Application.SearchQuery;
using Alcedo.Framework.Notification.Application.Service.MessageTemplate;
using Alcedo.Framework.Shared.Controller;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace Alcedo.Framework.Notification.Controller;

[Area("notification")]
[ApiVersion("1.0")]
[Resource("MessageTemplates", "Manage Notification Templates")]
public sealed class MessageTemplatesController : MasterDataControllerBase<MessageTemplateDto, MessageTemplateSearchQuery
    , MessageTemplateCreateCommand, MessageTemplateUpdateCommand>
{
    private readonly IMessageTemplateApplicationService _messageTemplateApplicationService;

    public MessageTemplatesController(IMessageTemplateApplicationService service) : base(service)
    {
        _messageTemplateApplicationService = service;
    }

    [HttpPost("{id}/test")]
    [Operation("Test", "Test rendering a template")]
    public async Task<ActionResult<MessageTemplateTestRenderResult>> TestRenderAsync([FromRoute] Guid id,
        [FromBody] string payload)
    {
        return Ok(await _messageTemplateApplicationService.TestMessageTemplateRenderAsync(id, payload));
    }
}