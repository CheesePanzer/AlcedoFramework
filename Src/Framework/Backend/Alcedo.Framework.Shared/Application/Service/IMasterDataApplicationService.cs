using Alcedo.Core.Application;
using Alcedo.Core.Application.SearchQuery;
using Alcedo.Framework.Shared.Application.Command;
using Alcedo.Framework.Shared.Application.Dto;
using Alcedo.Framework.Shared.Application.SearchQuery;

namespace Alcedo.Framework.Shared.Application.Service;

public interface
    IMasterDataApplicationService<TDto, in TSearch, in TCreateCommand, in TUpdateCommand> :
    IReferenceDataApplicationService<TDto, TSearch>
    where TDto : MasterDataDtoBase
    where TSearch : MasterDataSearchQueryBase
    where TCreateCommand : MasterDataCreateCommandBase
    where TUpdateCommand : MasterDataUpdateCommandBase

{
    Task<Guid> CreateAsync(TCreateCommand command, ContextOper oper, CancellationToken ct = default);

    Task UpdateByIdAsync(Guid id, TUpdateCommand command, ContextOper oper, CancellationToken ct = default);

    Task DeleteByIdAsync(Guid id, ContextOper oper, CancellationToken ct = default);

    Task EnableByIdAsync(Guid id, ContextOper oper, CancellationToken ct = default);

    Task DisableByIdAsync(Guid id, ContextOper oper, CancellationToken ct = default);

    Task<PagedResult<MasterDataAuditLogDto>> GetPagedAuditLogsAsync(MasterDataLogSearchQuery query, SortQuery sortQuery,
        PaginationQuery pagiQuery,
        CancellationToken ct = default);
}