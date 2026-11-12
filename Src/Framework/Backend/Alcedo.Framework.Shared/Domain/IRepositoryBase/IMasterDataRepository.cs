using Alcedo.Framework.Shared.Domain.Entity;

namespace Alcedo.Framework.Shared.Domain.IRepositoryBase;

public interface IMasterDataRepositoryBase<T> : IReferenceDataRepositoryBase<T>
    where T : MasterDataAggregateRootBase
{
    /// <summary>
    /// Only get enabled item as readonly, if not found then throw
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task<T?> GetEnabledByIdNoTrackingAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Only get enabled item but editable, if not found then throw
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task<T?> GetEnabledByIdAsync(Guid id, CancellationToken ct = default);

    Task<T?> GetEnabledByIdForUpdateAsync(Guid id, CancellationToken ct = default);

    Task<T?> GetEnabledByIdForShareAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Get readonly lists of all enabled data whose id is in id list; if not, the data wouldn't be returned
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task<IReadOnlyList<T>> GetEnabledListByIdNoTrackingAsync(IReadOnlyCollection<Guid> ids,
        CancellationToken ct = default);

    /// <summary>
    /// Get lists of all enabled data whose id is in id list; if not, the data wouldn't be returned. data is under tracking 
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task<List<T>> GetEnabledListByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default);

    Task<IReadOnlyList<T>> GetEnabledListStrictByIdNoTrackingAsync(IReadOnlyCollection<Guid> ids,
        CancellationToken ct = default);

    Task<List<T>> GetEnabledListPartialByIdsForUpdateAsync(IReadOnlyCollection<Guid> ids,
        CancellationToken ct = default);

    Task<IReadOnlyList<T>> GetEnabledListPartialByIdsForShareAsync(IReadOnlyCollection<Guid> ids,
        CancellationToken ct = default);

    Task<List<T>> GetEnabledListStrictByIdsForUpdateAsync(IReadOnlyCollection<Guid> ids,
        CancellationToken ct = default);

    Task<IReadOnlyList<T>> GetEnabledListStrictByIdsForShareAsync(IReadOnlyCollection<Guid> ids,
        CancellationToken ct = default);

    Task<List<T>> GetEnabledListStrictByIdsAsync(IReadOnlyCollection<Guid> ids,
        CancellationToken ct = default);
    
    Task<List<T>> GetRequiredStatusListStrictByIdsForUpdateAsync(IReadOnlyCollection<Guid> ids, IReadOnlyCollection<MasterDataStateValue> states,
        CancellationToken ct = default);
    
    Task<IReadOnlyList<T>> GetRequiredStatusListStrictByIdsForShareAsync(IReadOnlyCollection<Guid> ids,
        IReadOnlyCollection<MasterDataStateValue> states,
        CancellationToken ct = default);
    
    Task<List<T>> GetRequiredStatusListPartialByIdsForUpdateAsync(IReadOnlyCollection<Guid> ids, IReadOnlyCollection<MasterDataStateValue> states,
        CancellationToken ct = default);

    /// <summary>
    /// Check the data with this id if enabled
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task<bool> CheckIdIfEnabledAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Batch check the data with in the id list if enabled, return disabled or not found ids' list;
    /// </summary>
    /// <param name="ids"></param>
    /// <returns></returns>
    Task<IReadOnlyList<Guid>> CheckIdsIfEnabledAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default);
}