using System.Linq.Expressions;
using Alcedo.Core.Domain.Entity;

namespace Alcedo.Core.Domain.IRepositoryBase;

public interface IEntityStoreBase<TEntity>
    where TEntity : EntityBase
{
    /// <summary>
    /// Find one by id as readonly
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task<TEntity?> GetByIdNoTrackingAsync(Guid id,
        CancellationToken ct = default);

    /// <summary>
    /// Find by id and ent is under tracking; optimistic lock enabled by default;
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task<TEntity?> GetByIdAsync(Guid id,
        CancellationToken ct = default);

    /// <summary>
    /// Find by id for update
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task<TEntity?> GetByIdForUpdateAsync(Guid id,
        CancellationToken ct = default);

    /// <summary>
    /// Find by id for share as readonly
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task<TEntity?> GetByIdForShareAsync(Guid id,
        CancellationToken ct = default);

    /// <summary>
    /// Retrieves a read-only list of entities by their IDs.
    /// </summary>
    /// <param name="ids">A list of entity IDs.</param>
    /// <returns>A list of entities.</returns>
    Task<IReadOnlyList<TEntity>> GetListByIdsNoTrackingAsync(IReadOnlyCollection<Guid> ids,
        CancellationToken ct = default);

    /// <summary>
    /// Retrieves a list of entities by their IDs with optimistic concurrency control enabled by default.
    /// </summary>
    /// <param name="ids">A list of entity IDs.</param>
    /// <returns>A list of entities.</returns>
    Task<List<TEntity>> GetListByIdsAsync(IReadOnlyCollection<Guid> ids,
        CancellationToken ct = default);

    Task<IReadOnlyList<TEntity>> GetListStrictByIdsNoTrackingAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken ct = default);

    Task<List<TEntity>> GetListStrictByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken ct = default);

    /// <summary>
    /// Retrieves a list of entities by their IDs and applies a write lock.
    /// </summary>
    /// <param name="ids">A list of entity IDs.</param>
    /// <returns>A list of entities.</returns>
    Task<List<TEntity>> GetListPartialByIdsForUpdateAsync(IReadOnlyCollection<Guid> ids,
        CancellationToken ct = default);

    /// <summary>
    /// Retrieves a list of entities by their IDs and applies a read lock.
    /// </summary>
    /// <param name="ids">A list of entity IDs.</param>
    /// <returns>A list of entities.</returns>
    Task<IReadOnlyList<TEntity>> GetListPartialByIdsForShareAsync(IReadOnlyCollection<Guid> ids,
        CancellationToken ct = default);

    /// <summary>
    /// Retrieves a list of entities by their IDs and applies a write lock.
    /// </summary>
    /// <param name="ids">A list of entity IDs.</param>
    /// <returns>A list of entities.</returns>
    Task<List<TEntity>> GetListStrictByIdsForUpdateAsync(IReadOnlyCollection<Guid> ids,
        CancellationToken ct = default);

    /// <summary>
    /// Retrieves a list of entities by their IDs and applies a read lock.
    /// </summary>
    /// <param name="ids">A list of entity IDs.</param>
    /// <returns>A list of entities.</returns>
    Task<IReadOnlyList<TEntity>> GetListStrictByIdsForShareAsync(IReadOnlyCollection<Guid> ids,
        CancellationToken ct = default);

    /// <summary>
    /// add a ent
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    void Add(TEntity entity);

    void AddRange(IEnumerable<TEntity> entities);

    /// <summary>
    /// remove this ent
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    void Remove(TEntity entity);

    /// <summary>
    /// Batch remove
    /// </summary>
    /// <param name="entities"></param>
    /// <returns></returns>
    void RemoveRange(IEnumerable<TEntity> entities);

    Task<bool> ExistsAsync(
        Expression<Func<TEntity, bool>> predicate,
        Guid? excludeId = null,
        CancellationToken ct = default);
}