using Alcedo.Framework.Shared.Domain.Entity;

namespace Alcedo.Framework.Shared.Domain.IRepositoryBase;

public interface IMasterDataStatusValidator
{
    /// <summary>
    /// Check the data with this id if enabled
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task<bool> CheckIdIfEnabledAsync<TMasterData>(Guid id, CancellationToken ct = default)
        where TMasterData : MasterDataAggregateRootBase;

    /// <summary>
    /// Batch check the data with in the id list if enabled, return disabled or not found ids' list;
    /// </summary>
    /// <param name="ids"></param>
    /// <returns></returns>
    Task<IReadOnlyList<Guid>> CheckBatchIdsIfEnabledAsync<TMasterData>(IReadOnlyCollection<Guid> ids, CancellationToken ct = default)
        where TMasterData : MasterDataAggregateRootBase;
    
    Task<bool> CheckIfEnabledForShareAsync<TMasterData>(Guid id, CancellationToken ct = default) where TMasterData : MasterDataAggregateRootBase;

    Task<IReadOnlyList<Guid>> CheckBatchIdsIfEnabledForShareAsync<TMasterData>(
        IReadOnlyCollection<Guid> ids, CancellationToken ct = default)
        where TMasterData : MasterDataAggregateRootBase;
    
    Task CheckIdIfEnabledOrThrowAsync<TMasterData>(Guid id, CancellationToken ct = default)
        where TMasterData : MasterDataAggregateRootBase;

    Task CheckBatchIdsIfEnabledOrThrowAsync<TMasterData>(IReadOnlyCollection<Guid> ids, CancellationToken ct = default)
        where TMasterData : MasterDataAggregateRootBase;

    Task CheckIfEnabledForShareOrThrowAsync<TMasterData>(Guid id, CancellationToken ct = default)
        where TMasterData : MasterDataAggregateRootBase;

    Task CheckBatchIdsIfEnabledForShareOrThrowAsync<TMasterData>(IReadOnlyCollection<Guid> ids, CancellationToken ct = default)
        where TMasterData : MasterDataAggregateRootBase;
    
    Task ScanAndCheckEnabledOrThrowAsync<T>(T aggregate, CancellationToken ct = default)
        where T : MasterDataAggregateRootBase;

    Task ScanAndCheckEnabledForShareOrThrowAsync<T>(T aggregate, CancellationToken ct = default)
        where T : MasterDataAggregateRootBase;
}