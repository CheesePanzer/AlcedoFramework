using Alcedo.Core.Application;
using Alcedo.Core.Application.SearchQuery;
using Alcedo.Core.Application.Tools;
using Alcedo.Framework.Shared.Application.Components.ReferenceData;
using Alcedo.Framework.Shared.Application.Dto;
using Alcedo.Framework.Shared.Application.SearchQuery;
using Alcedo.Framework.Shared.Domain.Entity;
using Alcedo.Framework.Shared.Domain.IRepositoryBase;
using MapsterMapper;

namespace Alcedo.Framework.Shared.Application.Service;

public abstract class ReferenceDataApplicationServiceBase<TAgg, TDto, TSearch> : IReferenceDataApplicationService<TDto, TSearch>
where TAgg : ReferenceDataAggregateRootBase
where TDto : ReferenceDataDtoBase
where TSearch : ReferenceDataSearchQueryBase
{
    protected readonly ReferenceDataSearchServiceComponent<TAgg, TDto, TSearch> Search;
    
    protected readonly IUnitOfWorkExecutor Executor;

    protected readonly IReferenceDataRepositoryBase<TAgg> Repository;
    
    protected readonly IMapper Mapper;

    protected ReferenceDataApplicationServiceBase(
        ReferenceDataSearchServiceComponent<TAgg, TDto, TSearch> search, 
        IUnitOfWorkExecutor executor, 
        IReferenceDataRepositoryBase<TAgg> repository, 
        IMapper mapper)
    {
        Search = search;
        Executor = executor;
        Repository = repository;
        Mapper = mapper;
    }

    public virtual Task<PagedResult<TDto>> GetPagedDtosAsync(TSearch query, SortQuery sortQuery, PaginationQuery pagiQuery, CancellationToken ct = default)
        => Search.GetPagedDtosAsync(query, sortQuery, pagiQuery, ct);

    public virtual Task<TDto> GetDtoByIdAsync(Guid id, CancellationToken ct = default)
        => Search.GetDtoByIdAsync(id, ct);
}