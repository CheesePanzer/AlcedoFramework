using System.Data;
using Alcedo.Core.Application.SearchQuery;
using Alcedo.Core.Domain;
using Alcedo.Core.Infrastructure.EfCore;
using Alcedo.Core.Infrastructure.QueryRepositoryBase;
using Alcedo.Core.Infrastructure.SearchQuery;
using Alcedo.Framework.Shared.Application.Dto;
using Alcedo.Framework.Shared.Application.IQueryRepository;
using Alcedo.Framework.Shared.Application.SearchQuery;
using Alcedo.Framework.Shared.Domain.Entity;
using Microsoft.EntityFrameworkCore;

namespace Alcedo.Framework.Shared.Infrastructure.QueryRepository;

public sealed class MasterDataAuditLogQueryRepository<TMasterData> :
    EntityQueryRepositoryBase<MasterDataAuditLogEntity, MasterDataAuditLogDto, MasterDataLogSearchQuery>,
    IMasterDataAuditLogQueryRepository<TMasterData>
    where TMasterData : MasterDataAggregateRootBase
{
    public MasterDataAuditLogQueryRepository(AlcedoDbContext context) : base(context)
    {
    }

    public override async Task<(IReadOnlyList<MasterDataAuditLogDto> Items, int TotalCount)> GetPagedAsync(
        MasterDataLogSearchQuery query,
        PaginationQuery pagiQuery,
        SortQuery sortQuery,
        CancellationToken ct = default)
    {
        await using var tx = await Context.Database
            .BeginTransactionAsync(IsolationLevel.RepeatableRead, ct);

        var identity = AggregateRootMetadataCenter.GetMetaData<TMasterData>().Identity;

        var filter =
            SearchQueryBuilder.BuildFilter<MasterDataAuditLogEntity, MasterDataLogSearchQuery>(query,
                q => q.AggregateIdentity == identity);
        var orderBy = SearchQueryBuilder.BuildOrderByOrDefault<MasterDataAuditLogEntity>(sortQuery);

        var q = DbSet.AsNoTracking();
        q = filter(q);
        q = orderBy(q);

        var totalCount = await q.CountAsync(ct);

        var items = await Project(q
                .Skip((pagiQuery.PageNo - 1) * pagiQuery.PageSize)
                .Take(pagiQuery.PageSize))
            .ToListAsync(ct);

        return (items, totalCount);
    }
}