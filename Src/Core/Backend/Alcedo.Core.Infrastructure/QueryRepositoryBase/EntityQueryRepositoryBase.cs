using System.Collections.Concurrent;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;
using Alcedo.Core.Application.Dto;
using Alcedo.Core.Application.IQueryRepository;
using Alcedo.Core.Application.SearchQuery;
using Alcedo.Core.Domain.Entity;
using Alcedo.Core.Domain.IStorageService;
using Alcedo.Core.Infrastructure.EfCore;
using Alcedo.Core.Infrastructure.SearchQuery;
using Mapster;
using MassTransit.Internals;
using Microsoft.EntityFrameworkCore;

namespace Alcedo.Core.Infrastructure.QueryRepositoryBase;

public class EntityQueryRepositoryBase<TEntity, TDto, TQuery> : IEntityQueryRepository<TEntity, TDto, TQuery>
    where TEntity : EntityBase
    where TDto : EntityDtoBase
    where TQuery : EntitySearchQueryBase
{
    protected readonly AlcedoDbContext Context;
    protected readonly DbSet<TEntity> DbSet;

    public EntityQueryRepositoryBase(AlcedoDbContext context)
    {
        Context = context;
        DbSet = context.RawEntity<TEntity>();
    }
    
    protected virtual Expression<Func<TEntity, bool>>[] BuildCustomPredicates(TQuery query) => [];

    public virtual async Task<(IReadOnlyList<TDto> Items, int TotalCount)> GetPagedAsync(
        TQuery query,
        PaginationQuery pagiQuery,
        SortQuery sortQuery,
        CancellationToken ct = default)
    {
        await using var tx = await Context.Database
            .BeginTransactionAsync(IsolationLevel.RepeatableRead, ct); // For the consistency of count and data;

        var filter = SearchQueryBuilder.BuildFilter(query, BuildCustomPredicates(query));
        var orderBy = SearchQueryBuilder.BuildOrderByOrDefault<TEntity>(sortQuery);

        var q = DbSet.AsNoTracking();
        q = filter(q);
        q = orderBy(q);

        var totalCount = await q.CountAsync(ct);

        var items = await Project(q
                .Skip((pagiQuery.PageNo - 1) * pagiQuery.PageSize)
                .Take(pagiQuery.PageSize))
            .ToListAsync(ct);

        return (EnrichItems(items), totalCount);
    }

    public virtual async Task<IReadOnlyList<TDto>> GetListAsync(
        TQuery query,
        SortQuery sortQuery,
        CancellationToken ct = default)
    {
        var filter = SearchQueryBuilder.BuildFilter(query, BuildCustomPredicates(query));
        var orderBy = SearchQueryBuilder.BuildOrderByOrDefault<TEntity>(sortQuery);

        var q = DbSet.AsNoTracking();
        q = filter(q);
        q = orderBy(q);

        var items = await Project(q)
            .ToListAsync(ct);

        return EnrichItems(items);
    }

    public virtual async Task<TDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => EnrichItem(
            await Project(DbSet
                .AsNoTracking()
                .Where(e => e.Id == id))
            .SingleOrDefaultAsync(ct)
        );

    public virtual async Task<IReadOnlyList<TDto>> GetListByIdsAsync(IReadOnlyCollection<Guid> ids,
        CancellationToken ct = default)
        => EnrichItems(
            await Project(DbSet
                .AsNoTracking()
                .Where(e => ids.Contains(e.Id)))
            .ToListAsync(ct)
            );

    protected virtual IQueryable<TDto> Project(IQueryable<TEntity> query)
        => query.ProjectToType<TDto>();

    protected virtual TDto? EnrichItem(TDto? dto)
    {
        return dto;
    }

    protected virtual IReadOnlyList<TDto> EnrichItems(IReadOnlyList<TDto> items)
    {
        foreach (var item in items)
        {
            EnrichItem(item);
        }

        return items;
    }
}