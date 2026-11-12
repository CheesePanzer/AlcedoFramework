using Alcedo.Core.Infrastructure.EfCore;
using Alcedo.Framework.Shared.Domain.Entity;
using Alcedo.Framework.Shared.Domain.Exceptions;
using Alcedo.Framework.Shared.Domain.IRepositoryBase;
using Microsoft.EntityFrameworkCore;

namespace Alcedo.Framework.Shared.Infrastructure.Repository;

public class MasterDataRepositoryBase<T> : ReferenceDataRepositoryBase<T>, IMasterDataRepositoryBase<T>
    where T : MasterDataAggregateRootBase
{
    public MasterDataRepositoryBase(AlcedoDbContext context) : base(context)
    {
    }

    public async Task<T?> GetEnabledByIdNoTrackingAsync(Guid id, CancellationToken ct = default)
    {
        return await DbSet.Where(a => a.Id == id && a.Status == MasterDataStates.Enabled).AsNoTracking()
            .SingleOrDefaultAsync(cancellationToken: ct);
    }

    public async Task<T?> GetEnabledByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await DbSet.Where(a => a.Id == id && a.Status == MasterDataStates.Enabled)
            .SingleOrDefaultAsync(cancellationToken: ct);
    }

    public Task<T?> GetEnabledByIdForUpdateAsync(Guid id, CancellationToken ct = default)
        => LockById(id)
            .Where(x => x.Status, MasterDataStates.Enabled)
            .ForUpdate().BuildQuery()
            .SingleOrDefaultAsync(ct);

    public Task<T?> GetEnabledByIdForShareAsync(Guid id, CancellationToken ct = default)
        => LockById(id)
            .Where(x => x.Status, MasterDataStates.Enabled)
            .ForShare().BuildQuery()
            .SingleOrDefaultAsync(ct);

    public async Task<IReadOnlyList<T>> GetEnabledListByIdNoTrackingAsync(IReadOnlyCollection<Guid> ids,
        CancellationToken ct = default)
    {
        if (ids.Count == 0)
        {
            return new List<T>();
        }

        return await DbSet.Where(a => ids.Contains(a.Id) && a.Status == MasterDataStates.Enabled)
            .AsNoTracking()
            .ToListAsync(cancellationToken: ct);
    }

    public async Task<List<T>> GetEnabledListByIdsAsync(IReadOnlyCollection<Guid> ids,
        CancellationToken ct = default)
    {
        if (ids.Count == 0)
        {
            return new List<T>();
        }

        return await DbSet.Where(a => ids.Contains(a.Id) && a.Status == MasterDataStates.Enabled)
            .ToListAsync(cancellationToken: ct);
    }

    public async Task<IReadOnlyList<T>> GetEnabledListStrictByIdNoTrackingAsync(IReadOnlyCollection<Guid> ids,
        CancellationToken ct = default)
    {
        if (ids.Count == 0)
        {
            return new List<T>();
        }

        var result = await DbSet.Where(a => ids.Contains(a.Id) && a.Status == MasterDataStates.Enabled)
            .AsNoTracking()
            .ToListAsync(cancellationToken: ct);

        var missingIds = ids.Except(result.Select(e => e.Id)).ToList();
        if (missingIds.Count > 0)
        {
            throw new MdErrResourceNotFoundOrNotEnabled<T>(missingIds);
        }

        return result;
    }

    public async Task<List<T>> GetEnabledListStrictByIdsAsync(IReadOnlyCollection<Guid> ids,
        CancellationToken ct = default)
    {
        if (ids.Count == 0)
        {
            return new List<T>();
        }

        var result = await DbSet.Where(a => ids.Contains(a.Id) && a.Status == MasterDataStates.Enabled)
            .ToListAsync(cancellationToken: ct);
        var missingIds = ids.Except(result.Select(e => e.Id)).ToList();
        if (missingIds.Count > 0)
        {
            throw new MdErrResourceNotFoundOrNotEnabled<T>(missingIds);
        }

        return result;
    }

    public async Task<List<T>> GetRequiredStatusListStrictByIdsForUpdateAsync(IReadOnlyCollection<Guid> ids,
        IReadOnlyCollection<MasterDataStateValue> states, CancellationToken ct = default)
    {
        var result = await LockByIds(ids)
            .WhereIn(x => x.Status, states)
            .ForUpdate().BuildQuery()
            .ToListAsync(ct);
        var missingIds = ids.Except(result.Select(e => e.Id)).ToList();
        if (missingIds.Count > 0)
        {
            throw new MdErrRequiredStatusResourceNotFound<T>(missingIds,states);
        }

        return result;
    }
    
    public async Task<IReadOnlyList<T>> GetRequiredStatusListStrictByIdsForShareAsync(IReadOnlyCollection<Guid> ids,
        IReadOnlyCollection<MasterDataStateValue> states, CancellationToken ct = default)
    {
        var result = await LockByIds(ids)
            .WhereIn(x => x.Status, states)
            .ForShare().BuildQuery()
            .ToListAsync(ct);
        var missingIds = ids.Except(result.Select(e => e.Id)).ToList();
        if (missingIds.Count > 0)
        {
            throw new MdErrRequiredStatusResourceNotFound<T>(missingIds,states);
        }

        return result;
    }
    
    public  Task<List<T>> GetRequiredStatusListPartialByIdsForUpdateAsync(IReadOnlyCollection<Guid> ids,
        IReadOnlyCollection<MasterDataStateValue> states, CancellationToken ct = default)
    =>  LockByIds(ids)
            .WhereIn(x => x.Status, states)
            .ForUpdate().BuildQuery()
            .ToListAsync(ct);



    public Task<List<T>> GetEnabledListPartialByIdsForUpdateAsync(IReadOnlyCollection<Guid> ids,
        CancellationToken ct = default) =>
        LockByIds(ids)
            .Where(x => x.Status, MasterDataStates.Enabled)
            .ForUpdate().BuildQuery()
            .ToListAsync(ct);

    public async Task<IReadOnlyList<T>> GetEnabledListPartialByIdsForShareAsync(IReadOnlyCollection<Guid> ids,
        CancellationToken ct = default) =>
        await LockByIds(ids)
            .Where(x => x.Status, MasterDataStates.Enabled)
            .ForShare().BuildQuery()
            .ToListAsync(ct);

    public async Task<List<T>> GetEnabledListStrictByIdsForUpdateAsync(IReadOnlyCollection<Guid> ids,
        CancellationToken ct = default)
    {
        var result = await LockByIds(ids)
            .Where(x => x.Status, MasterDataStates.Enabled)
            .ForUpdate().BuildQuery()
            .ToListAsync(ct);

        var missingIds = ids.Except(result.Select(e => e.Id)).ToList();
        if (missingIds.Count > 0)
        {
            throw new MdErrResourceNotFoundOrNotEnabled<T>(missingIds);
        }

        return result;
    }

    public async Task<IReadOnlyList<T>> GetEnabledListStrictByIdsForShareAsync(IReadOnlyCollection<Guid> ids,
        CancellationToken ct = default)
    {
        var result = await LockByIds(ids)
            .Where(x => x.Status, MasterDataStates.Enabled)
            .ForShare().BuildQuery()
            .ToListAsync(ct);

        var missingIds = ids.Except(result.Select(e => e.Id)).ToList();
        if (missingIds.Count > 0)
        {
            throw new MdErrResourceNotFoundOrNotEnabled<T>(missingIds);
        }

        return result;
    }

    public async Task<bool> CheckIdIfEnabledAsync(Guid id, CancellationToken ct = default)
    {
        return await DbSet.AnyAsync(a => a.Id == id && a.Status == MasterDataStates.Enabled,
            cancellationToken: ct);
    }

    public async Task<IReadOnlyList<Guid>> CheckIdsIfEnabledAsync(IReadOnlyCollection<Guid> ids,
        CancellationToken ct = default)
    {
        if (ids.Count == 0)
        {
            return new List<Guid>();
        }

        var enabledIds = await DbSet
            .Where(a => ids.Contains(a.Id) && a.Status == MasterDataStates.Enabled)
            .Select(a => a.Id)
            .ToListAsync(cancellationToken: ct);

        var invalidIds = ids.Except(enabledIds).ToList();

        return invalidIds;
    }
}