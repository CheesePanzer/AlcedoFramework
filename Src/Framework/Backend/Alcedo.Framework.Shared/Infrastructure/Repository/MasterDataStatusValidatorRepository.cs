using Alcedo.Core.Domain.ReferenceIntegrity;
using Alcedo.Core.Infrastructure.EfCore;
using Alcedo.Core.Infrastructure.RepositoryBase;
using Alcedo.Framework.Shared.Domain.Entity;
using Alcedo.Framework.Shared.Domain.Exceptions;
using Alcedo.Framework.Shared.Domain.IRepositoryBase;
using Microsoft.EntityFrameworkCore;

namespace Alcedo.Framework.Shared.Infrastructure.Repository;

internal sealed class MasterDataStatusValidator : IMasterDataStatusValidator
{
    private readonly AlcedoDbContext _dbContext;
    
    private readonly IAggRefScanner _aggRefScanner;

    public MasterDataStatusValidator(AlcedoDbContext dbContext, IAggRefScanner aggRefScanner)
    {
        _dbContext = dbContext;
        _aggRefScanner = aggRefScanner;
    }

    public Task<bool> CheckIdIfEnabledAsync<TMasterData>(Guid id, CancellationToken ct = default) where TMasterData : MasterDataAggregateRootBase
        => _dbContext.AggRoot<TMasterData>()
            .AnyAsync(e => e.Id == id && e.Status == MasterDataStates.Enabled, ct);
    
    public async Task<IReadOnlyList<Guid>> CheckBatchIdsIfEnabledAsync<TMasterData>(IReadOnlyCollection<Guid> ids, CancellationToken ct = default) where TMasterData : MasterDataAggregateRootBase
    {
        if (ids.Count == 0) return [];

        var enabledIds = await _dbContext.AggRoot<TMasterData>()
            .Where(e => ids.Contains(e.Id) && e.Status == MasterDataStates.Enabled)
            .Select(e => e.Id)
            .ToListAsync(ct);

        return ids.Except(enabledIds).ToList();
    }

    public async Task<bool> CheckIfEnabledForShareAsync<TMasterData>(Guid id, CancellationToken ct = default)
        where TMasterData : MasterDataAggregateRootBase
    {
        return await new PessimisticLockQueryBuilder<TMasterData>(_dbContext)
            .Where(x => x.Id, id)
            .Where(x => x.Status, MasterDataStates.Enabled)
            .ForShare()
            .BuildQuery()
            .AnyAsync(ct);
    }
    
    public async Task<IReadOnlyList<Guid>> CheckBatchIdsIfEnabledForShareAsync<TMasterData>(
        IReadOnlyCollection<Guid> ids, CancellationToken ct = default)
        where TMasterData : MasterDataAggregateRootBase
    {
        if (ids.Count == 0) return [];

        var enabledIds = await new PessimisticLockQueryBuilder<TMasterData>(_dbContext)
            .WhereIn(x => x.Id, ids)
            .Where(x => x.Status, MasterDataStates.Enabled)
            .ForShare()
            .BuildQuery()
            .Select(x => x.Id)
            .ToListAsync(ct);

        return ids.Except(enabledIds).ToList();
    }
    
    public async Task CheckIdIfEnabledOrThrowAsync<TMasterData>(Guid id, CancellationToken ct = default)
        where TMasterData : MasterDataAggregateRootBase
    {
        if (!await CheckIdIfEnabledAsync<TMasterData>(id, ct))
            throw new MdErrResourceNotFoundOrNotEnabled<TMasterData>(id);
    }

    public async Task CheckBatchIdsIfEnabledOrThrowAsync<TMasterData>(IReadOnlyCollection<Guid> ids, CancellationToken ct = default)
        where TMasterData : MasterDataAggregateRootBase
    {
        var invalidIds = await CheckBatchIdsIfEnabledAsync<TMasterData>(ids, ct);
        if (invalidIds.Any())
            throw new MdErrResourceNotFoundOrNotEnabled<TMasterData>(invalidIds);
    }

    public async Task CheckIfEnabledForShareOrThrowAsync<TMasterData>(Guid id, CancellationToken ct = default)
        where TMasterData : MasterDataAggregateRootBase
    {
        if (!await CheckIfEnabledForShareAsync<TMasterData>(id, ct))
            throw new MdErrResourceNotFoundOrNotEnabled<TMasterData>(id);
    }

    public async Task CheckBatchIdsIfEnabledForShareOrThrowAsync<TMasterData>(IReadOnlyCollection<Guid> ids, CancellationToken ct = default)
        where TMasterData : MasterDataAggregateRootBase
    {
        var invalidIds = await CheckBatchIdsIfEnabledForShareAsync<TMasterData>(ids, ct);
        if (invalidIds.Any())
            throw new MdErrResourceNotFoundOrNotEnabled<TMasterData>(invalidIds);
    }
    
    public async Task ScanAndCheckEnabledOrThrowAsync<T>(T aggregate, CancellationToken ct = default)
        where T : MasterDataAggregateRootBase
    {
        var typeIds = _aggRefScanner.Scan(aggregate)
            .Where(kv => typeof(MasterDataAggregateRootBase).IsAssignableFrom(kv.Key))
            .ToDictionary(kv => kv.Key, kv => kv.Value);

        if (typeIds.Count == 0) return;

        await CheckMultiTypeOrThrowAsync(typeIds, ct);
    }

    public async Task ScanAndCheckEnabledForShareOrThrowAsync<T>(T aggregate, CancellationToken ct = default)
        where T : MasterDataAggregateRootBase
    {
        var typeIds = _aggRefScanner.Scan(aggregate)
            .Where(kv => typeof(MasterDataAggregateRootBase).IsAssignableFrom(kv.Key))
            .ToDictionary(kv => kv.Key, kv => kv.Value);

        if (typeIds.Count == 0) return;

        await CheckMultiTypeForShareOrThrowAsync(typeIds, ct);
    }
    
    public async Task CheckMultiTypeOrThrowAsync(
        IReadOnlyDictionary<Type, IReadOnlyCollection<Guid>> typeIds,
        CancellationToken ct = default)
    {
        foreach (var (type, ids) in typeIds)
        {
            if (ids.Count == 0) continue;

            var genericMethod = typeof(MasterDataStatusValidator)
                .GetMethod(nameof(CheckBatchIdsIfEnabledAsync))!
                .MakeGenericMethod(type);

            var invalidIds = await (Task<IReadOnlyList<Guid>>)genericMethod.Invoke(this, [ids, ct])!;

            if (invalidIds.Any())
                throw new MdErrResourceNotFoundOrNotEnabled(type, invalidIds);
        }
    }

    public async Task CheckMultiTypeForShareOrThrowAsync(
        IReadOnlyDictionary<Type, IReadOnlyCollection<Guid>> typeIds,
        CancellationToken ct = default)
    {
        foreach (var (type, ids) in typeIds)
        {
            if (ids.Count == 0) continue;

            var genericMethod = typeof(MasterDataStatusValidator)
                .GetMethod(nameof(CheckBatchIdsIfEnabledForShareAsync))!
                .MakeGenericMethod(type);

            var invalidIds = await (Task<IReadOnlyList<Guid>>)genericMethod.Invoke(this, [ids, ct])!;

            if (invalidIds.Any())
                throw new MdErrResourceNotFoundOrNotEnabled(type, invalidIds);
        }
    }
}