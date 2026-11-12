using Alcedo.Core.Application;
using Alcedo.Core.Application.SearchQuery;
using Alcedo.Core.Presentation;
using Alcedo.Framework.Notification.Application.Dto;
using Alcedo.Framework.Notification.Application.SearchQuery;
using Alcedo.Framework.Notification.Application.Service.MessageError;
using Alcedo.Framework.Shared.Controller;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace Alcedo.Framework.Notification.Controller;

[Area("notification")]
[ApiVersion("1.0")]
[Resource("MessageErrors", "Manage Notification Errors")]
public class MessageErrorController : AlcedoFmApiAreaControllerBase
{
    private readonly IMessageErrorApplicationService _service;

    public MessageErrorController(IMessageErrorApplicationService messageErrorApplicationService)
    {
        _service = messageErrorApplicationService;
    }
    
    [HttpGet]
    [Operation("PageView", "View Items' List")]
    public virtual async Task<ActionResult<PagedResult<MessageErrorDto>>> GetPagedItems([FromQuery] MessageErrorSearchQuery query,
        [FromQuery] SortQuery sortQuery, [FromQuery] PaginationQuery paginationQuery,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _service.GetPagedDtosAsync(query, sortQuery, paginationQuery, cancellationToken));
    }

    [HttpGet("{id}")]
    [Operation("DetailView", "View Item's Detail")]
    public virtual async Task<ActionResult<MessageErrorDto>> GetItemInfo(Guid id,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _service.GetDtoByIdAsync(id, cancellationToken));
    }
    
    [HttpPut("{id}/event:retry")]
    [Operation("ConvertToEvent", "Retry sending this event")]
    public virtual async Task<IActionResult> RetryErrorMsg(Guid id,
        CancellationToken cancellationToken = default)
    {
        await _service.ConvertToEventAsync(id, cancellationToken);
        return NoContent();
    }
    
    [HttpDelete("{id}")]
    [Operation("RemoveEvent", "Remove this error event")]
    public virtual async Task<IActionResult> DeleteErrorMsg(Guid id,
        CancellationToken cancellationToken = default)
    {
        await _service.IgnoreErrorAsync(id, cancellationToken);
        return NoContent();
    }
}