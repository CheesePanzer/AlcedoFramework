using Alcedo.Core.Application;
using Alcedo.Core.Application.SearchQuery;
using Alcedo.Core.Presentation;
using Alcedo.Framework.Shared.Application;
using Alcedo.Framework.Shared.Application.Command;
using Alcedo.Framework.Shared.Application.Dto;
using Alcedo.Framework.Shared.Application.SearchQuery;
using Alcedo.Framework.Shared.Application.Service;
using Microsoft.AspNetCore.Mvc;

namespace Alcedo.Framework.Shared.Controller;

public abstract class
    MasterDataControllerBase<TDto, TSearch, TCreateCommand, TUpdateCommand> : ReferenceDataApiControllerBase<
    TDto,
    TSearch>
    where TDto : MasterDataDtoBase
    where TSearch : MasterDataSearchQueryBase
    where TCreateCommand : MasterDataCreateCommandBase
    where TUpdateCommand : MasterDataUpdateCommandBase
{
    private readonly IMasterDataApplicationService<TDto, TSearch, TCreateCommand, TUpdateCommand> _service;

    protected MasterDataControllerBase(
        IMasterDataApplicationService<TDto, TSearch, TCreateCommand, TUpdateCommand> service) : base(service)
    {
        _service = service;
    }

    [HttpPost]
    [Operation("Create", "Create New Item")]
    public async Task<ActionResult<Guid>> CreateItem(TCreateCommand command,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _service.CreateAsync(command, CurrentUser, cancellationToken));
    }

    [HttpPut("{id}")]
    [Operation("Update", "Update An Item")]
    public async Task<IActionResult> UpdateItem(Guid id, TUpdateCommand command,
        CancellationToken cancellationToken = default)
    {
        await _service.UpdateByIdAsync(id, command, CurrentUser, cancellationToken);
        return Ok();
    }

    [HttpPatch("{id}/status:enable")]
    [Operation("Enable", "Enable Item")]
    public virtual async Task<IActionResult> EnableItem(Guid id, CancellationToken cancellationToken = default)
    {
        await _service.EnableByIdAsync(id, CurrentUser, cancellationToken);
        return Ok();
    }

    [HttpPatch("{id}/status:disable")]
    [Operation("Disable", "Disable Item")]
    public virtual async Task<IActionResult> DisableItem(Guid id, CancellationToken cancellationToken = default)
    {
        await _service.DisableByIdAsync(id, CurrentUser, cancellationToken);
        return Ok();
    }

    [HttpDelete("{id}")]
    [Operation("Delete", "Delete an item")]
    public virtual async Task<IActionResult> DeleteItem(Guid id, CancellationToken cancellationToken = default)
    {
        await _service.DeleteByIdAsync(id, CurrentUser, cancellationToken);
        return Ok();
    }

    [HttpGet("logs")]
    [Operation("ViewAuditLogs", "View Item's Audit Log")]
    public async Task<ActionResult<PagedResult<MasterDataAuditLogDto>>> GetAuditLogs(
        [FromQuery] MasterDataLogSearchQuery query,
        [FromQuery] SortQuery sortQuery, [FromQuery] PaginationQuery paginationQuery,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _service.GetPagedAuditLogsAsync(query, sortQuery, paginationQuery, cancellationToken));
    }
}