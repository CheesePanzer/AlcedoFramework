using Alcedo.Core.Application;
using Alcedo.Core.Application.SearchQuery;
using Alcedo.Core.Application.Tools;
using Alcedo.Framework.Shared.Application.Command;
using Alcedo.Framework.Shared.Application.Components.MasterData;
using Alcedo.Framework.Shared.Application.Dto;
using Alcedo.Framework.Shared.Application.SearchQuery;
using Alcedo.Framework.Shared.Domain.Entity;
using Alcedo.Framework.Shared.Domain.IRepositoryBase;
using MapsterMapper;

namespace Alcedo.Framework.Shared.Application.Service;

public abstract class MasterDataApplicationServiceBase<TAgg, TDto, TSearch, TCreateCommand, TUpdateCommand> : ReferenceDataApplicationServiceBase<TAgg, TDto, TSearch>, IMasterDataApplicationService<TDto, TSearch, TCreateCommand, TUpdateCommand>
    where TAgg : MasterDataAggregateRootBase
    where TDto : MasterDataDtoBase
    where TSearch : MasterDataSearchQueryBase 
    where TCreateCommand : MasterDataCreateCommandBase
    where TUpdateCommand : MasterDataUpdateCommandBase
{
    protected readonly MasterDataStatusServiceComponent<TAgg> Status;

    protected readonly MasterDataLogSearchService<TAgg> Log;

    protected readonly MasterDataEditServiceComponent<TAgg, TCreateCommand, TUpdateCommand> Edit;
    
    public MasterDataApplicationServiceBase(
        MasterDataSearchServiceComponent<TAgg, TDto, TSearch> search, 
        IUnitOfWorkExecutor executor, 
        IMasterDataRepositoryBase<TAgg> repository, 
        IMapper mapper, 
        MasterDataStatusServiceComponent<TAgg> status, 
        MasterDataLogSearchService<TAgg> log, 
        MasterDataEditServiceComponent<TAgg, TCreateCommand, TUpdateCommand> edit) : base(search, executor, repository, mapper)
    {
        Status = status;
        Log = log;
        Edit = edit;
    }

    public virtual Task<Guid> CreateAsync(TCreateCommand command, ContextOper oper, CancellationToken ct = default)
    => Edit.CreateAsync(command, oper, ct);

    public virtual Task UpdateByIdAsync(Guid id, TUpdateCommand command, ContextOper oper, CancellationToken ct = default)
    => Edit.UpdateByIdAsync(id, command, oper, ct);

    public virtual Task DeleteByIdAsync(Guid id, ContextOper oper, CancellationToken ct = default)
    => Edit.DeleteByIdAsync(id, oper, ct);

    public virtual Task EnableByIdAsync(Guid id, ContextOper oper, CancellationToken ct = default)
    => Status.EnableByIdAsync(id, oper, ct);

    public virtual Task DisableByIdAsync(Guid id, ContextOper oper, CancellationToken ct = default)
    => Status.DisableByIdAsync(id, oper, ct);

    public Task<PagedResult<MasterDataAuditLogDto>> GetPagedAuditLogsAsync(MasterDataLogSearchQuery query, SortQuery sortQuery, PaginationQuery pagiQuery,
        CancellationToken ct = default)
    => Log.GetPagedDtosAsync(query, sortQuery, pagiQuery, ct);
}