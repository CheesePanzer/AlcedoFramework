using Alcedo.Frontend.Abstraction.Command;
using Alcedo.Frontend.Abstraction.Dto;
using Alcedo.Frontend.Abstraction.SearchQuery;

namespace Alcedo.Frontend.Abstraction.BackednInterface;

public interface IMasterDataController<TDto, in TSearch, in TCreateCommand, in TUpdateCommand> :
    IMasterDataEditController<TCreateCommand, TUpdateCommand>, IMasterDataLogController, IMasterDataStateController,
    IReferenceController<TDto, TSearch>
    where TDto : MasterDataDtoBase
    where TSearch : MasterDataSearchQueryBase
    where TCreateCommand : MasterDataCreateCommandBase
    where TUpdateCommand : MasterDataUpdateCommandBase
{
}

public interface IMasterDataEditController<in TCreateCommand, in TUpdateCommand>
    where TCreateCommand : MasterDataCreateCommandBase
    where TUpdateCommand : MasterDataUpdateCommandBase
{
    [HttpPost]
    Task<Guid> CreateAsync([ToBody] TCreateCommand command);

    [HttpPut("{id}")]
    Task UpdateAsync([ToRoute] Guid id, [ToBody] TUpdateCommand command);
}

public interface IMasterDataLogController
{
    [HttpGet("logs")]
    Task<PagedResult<MasterDataAuditLogDto>> GetAuditLogsAsync(
        MasterDataLogSearchQuery query,
        SortQuery sortQuery,
        PaginationQuery paginationQuery);
}

public interface IMasterDataStateController
{
    [HttpDelete("{id}")]
    Task DeleteAsync([ToRoute] Guid id);

    [HttpPatch("{id}/status:enable")]
    Task EnableAsync([ToRoute] Guid id);

    [HttpPatch("{id}/status:disable")]
    Task DisableAsync([ToRoute] Guid id);
}