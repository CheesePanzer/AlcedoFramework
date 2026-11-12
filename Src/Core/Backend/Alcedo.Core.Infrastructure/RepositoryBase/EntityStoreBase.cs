using System.Linq.Expressions;
using Alcedo.Core.Domain.DomException;
using Alcedo.Core.Domain.Entity;
using Alcedo.Core.Domain.IRepositoryBase;
using Alcedo.Core.Infrastructure.EfCore;
using Microsoft.EntityFrameworkCore;

namespace Alcedo.Core.Infrastructure.RepositoryBase;

public class EntityStoreBase<TEntity> : IEntityStoreBase<TEntity>
    where TEntity : EntityBase
{
    protected readonly AlcedoDbContext Context;
    protected readonly DbSet<TEntity> DbSet;

    public EntityStoreBase(AlcedoDbContext context)
    {
        Context = context;
        DbSet = context.RawEntity<TEntity>();
    }

    public virtual async Task<TEntity?> GetByIdNoTrackingAsync(Guid id,
        CancellationToken ct = default)
    {
        return await DbSet
            .Where(b => b.Id == id)
            .AsNoTracking()
            .SingleOrDefaultAsync(ct);
    }

    public virtual async Task<TEntity?> GetByIdAsync(Guid id,
        CancellationToken ct = default)
    {
        return await DbSet
            .Where(b => b.Id == id)
            .SingleOrDefaultAsync(ct);
    }

    public virtual async Task<TEntity?> GetByIdForUpdateAsync(Guid id,
        CancellationToken ct = default) =>
        await LockById(id).ForUpdate().BuildQuery().SingleOrDefaultAsync(ct);

    public virtual async Task<TEntity?> GetByIdForShareAsync(Guid id,
        CancellationToken ct = default) =>
        await LockById(id).ForShare().BuildQuery().SingleOrDefaultAsync(ct);

    public virtual async Task<IReadOnlyList<TEntity>> GetListByIdsNoTrackingAsync(IReadOnlyCollection<Guid> ids,
        CancellationToken ct = default)
    {
        if (ids.Count == 0)
        {
            return new List<TEntity>();
        }

        return await DbSet
            .Where(b => ids.Contains(b.Id))
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public virtual async Task<List<TEntity>> GetListByIdsAsync(IReadOnlyCollection<Guid> ids,
        CancellationToken ct = default)
    {
        if (ids.Count == 0)
        {
            return new List<TEntity>();
        }

        return await DbSet
            .Where(b => ids.Contains(b.Id))
            .ToListAsync(ct);
    }

    public virtual async Task<IReadOnlyList<TEntity>> GetListStrictByIdsNoTrackingAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken ct = default)
    {
        if (ids.Count == 0)
        {
            return new List<TEntity>();
        }

        var result = await DbSet
            .Where(b => ids.Contains(b.Id))
            .AsNoTracking()
            .ToListAsync(ct);

        var missingIds = ids.Except(result.Select(e => e.Id)).ToList();
        if (missingIds.Count > 0)
        {
            throw new DomErrResourceNotFound<TEntity>(missingIds);
        }

        return result;
    }

    public virtual async Task<List<TEntity>> GetListStrictByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken ct = default)
    {
        if (ids.Count == 0)
        {
            return new List<TEntity>();
        }

        var result = await DbSet
            .Where(b => ids.Contains(b.Id))
            .ToListAsync(ct);

        var missingIds = ids.Except(result.Select(e => e.Id)).ToList();
        if (missingIds.Count > 0)
        {
            throw new DomErrResourceNotFound<TEntity>(missingIds);
        }

        return result;
    }

    public virtual Task<List<TEntity>> GetListPartialByIdsForUpdateAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken ct = default)
        => LockByIds(ids)
            .ForUpdate()
            .BuildQuery()
            .ToListAsync(ct);

    public virtual async Task<IReadOnlyList<TEntity>> GetListPartialByIdsForShareAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken ct = default)
        => await LockByIds(ids)
            .ForShare()
            .BuildQuery()
            .ToListAsync(ct);

    public virtual async Task<List<TEntity>> GetListStrictByIdsForUpdateAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken ct = default)
    {
        var result = await LockByIds(ids)
            .ForUpdate()
            .BuildQuery()
            .ToListAsync(ct);

        var missingIds = ids.Except(result.Select(e => e.Id)).ToList();
        if (missingIds.Count > 0)
        {
            throw new DomErrResourceNotFound<TEntity>(missingIds);
        }

        return result;
    }

    public virtual async Task<IReadOnlyList<TEntity>> GetListStrictByIdsForShareAsync(IReadOnlyCollection<Guid> ids,
        CancellationToken ct = default)
    {
        var result = await LockByIds(ids)
            .ForShare()
            .BuildQuery()
            .ToListAsync(ct);

        var missingIds = ids.Except(result.Select(e => e.Id)).ToList();
        if (missingIds.Count > 0)
        {
            throw new DomErrResourceNotFound<TEntity>(missingIds);
        }

        return result;
    }

    public void Add(TEntity entity)
    {
        DbSet.Add(entity);
    }

    public void AddRange(IEnumerable<TEntity> entities)
    {
        DbSet.AddRange(entities);
    }

    public virtual void Remove(TEntity entity)
    {
        DbSet.Remove(entity);
    }

    public virtual void RemoveRange(IEnumerable<TEntity> entities)
    {
        DbSet.RemoveRange(entities);
    }

    protected PessimisticLockQueryBuilder<TEntity> Lock()
        => new(Context);

    protected PessimisticLockQueryBuilder<TEntity> LockById(Guid id)
        => Lock().Where(x => x.Id, id);

    protected PessimisticLockQueryBuilder<TEntity> LockByIds(IReadOnlyCollection<Guid> ids)
        => Lock().WhereIn(x => x.Id, ids);

    public virtual async Task<bool> ExistsAsync(
        Expression<Func<TEntity, bool>> predicate,
        Guid? excludeId = null,
        CancellationToken ct = default)
    {
        var query = DbSet.Where(predicate);

        if (excludeId.HasValue)
        {
            query = query.Where(e => e.Id != excludeId.Value);
        }

        return await query.AnyAsync(ct);
    }
}