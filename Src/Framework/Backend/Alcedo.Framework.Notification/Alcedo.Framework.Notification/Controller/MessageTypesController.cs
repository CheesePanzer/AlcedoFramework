using Alcedo.Core.Application;
using Alcedo.Core.Application.SearchQuery;
using Alcedo.Core.Presentation;
using Alcedo.Framework.Notification.Application.Dto;
using Alcedo.Framework.Notification.Application.SearchQuery;
using Alcedo.Framework.Notification.Application.Service.MessageType;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace Alcedo.Framework.Notification.Controller;

[Area("notification")]
[ApiVersion("1.0")]
[Resource("MessageType", "Manage Site's Message Type runtime configuration")]
public sealed class MessageTypesController : AlcedoApiAreaControllerBase
{
    private readonly IMessageTypeApplicationService _service;

    public MessageTypesController(IMessageTypeApplicationService service)
    {
        _service = service;
    }

    [HttpGet]
    [Operation("PageView", "View Items' List")]
    public ActionResult<PagedResult<MessageTypeDto>> GetPagedItems([FromQuery] MessageTypeSearchQuery query,
        [FromQuery] PaginationQuery paginationQuery)
    {
        return Ok(_service.GetPagedDtosAsync(query, paginationQuery));
    }
    
    [HttpGet("{code}")]
    [Operation("DetailView", "View Item's Detail With Same Code")]
    public ActionResult<MessageTypeDto> GetItemByCode([FromRoute] string code)
    {
        return Ok(_service.GetDtoByCode(code));
    }
}