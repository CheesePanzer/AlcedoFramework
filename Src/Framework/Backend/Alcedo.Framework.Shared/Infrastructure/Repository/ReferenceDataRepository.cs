using Alcedo.Core.Domain.DomException;
using Alcedo.Core.Infrastructure.EfCore;
using Alcedo.Core.Infrastructure.RepositoryBase;
using Alcedo.Framework.Shared.Domain.Entity;
using Alcedo.Framework.Shared.Domain.IRepositoryBase;
using Microsoft.EntityFrameworkCore;

namespace Alcedo.Framework.Shared.Infrastructure.Repository;

public class ReferenceDataRepositoryBase<T> : AggregateRootRepositoryBase<T>, IReferenceDataRepositoryBase<T>
    where T : ReferenceDataAggregateRootBase
{
    public ReferenceDataRepositoryBase(AlcedoDbContext context) : base(context)
    {
    }

    public virtual async Task<bool> CheckIfCodeExistsAsync(string code, CancellationToken ct = default)
    {
        return await DbSet.AnyAsync(x => x.Code == code, ct);
    }

    public virtual async Task<T?> GetByCodeNoTrackingAsync(string code, CancellationToken ct = default)
    {
        return await DbSet.AsNoTracking().SingleOrDefaultAsync(x => x.Code == code, ct);
    }

    public virtual async Task<T?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        return await DbSet.SingleOrDefaultAsync(x => x.Code == code, ct);
    }

    public virtual async Task<T?> GetByCodeForUpdateAsync(string code,
        CancellationToken ct = default) =>
        await LockByCode(code).ForUpdate().BuildQuery().SingleOrDefaultAsync(ct);

    public virtual async Task<T?> GetByCodeForShareAsync(string code,
        CancellationToken ct = default) =>
        await LockByCode(code).ForShare().BuildQuery().SingleOrDefaultAsync(ct);

    protected PessimisticLockQueryBuilder<T> LockByCode(string code)
        => Lock().Where(x => x.Code, code);

    public virtual async Task<IReadOnlyList<T>> GetListByCodesNoTrackingAsync(IReadOnlyCollection<string> codes,
        CancellationToken ct = default)
    {
        if (codes.Count == 0)
        {
            return new List<T>();
        }

        return await DbSet
            .Where(b => codes.Contains(b.Code))
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public virtual async Task<List<T>> GetListByCodesAsync(IReadOnlyCollection<string> codes,
        CancellationToken ct = default)
    {
        if (codes.Count == 0)
        {
            return new List<T>();
        }

        return await DbSet
            .Where(b => codes.Contains(b.Code))
            .ToListAsync(ct);
    }

    public virtual Task<List<T>> GetListPartialByCodesForUpdateAsync(
        IReadOnlyCollection<string> codes,
        CancellationToken ct = default)
        => LockByCodes(codes)
            .ForUpdate()
            .BuildQuery()
            .ToListAsync(ct);

    public virtual async Task<IReadOnlyList<T>> GetListPartialByCodesForShareAsync(
        IReadOnlyCollection<string> codes,
        CancellationToken ct = default)
        => await LockByCodes(codes)
            .ForShare()
            .BuildQuery()
            .ToListAsync(ct);

    public virtual async Task<List<T>> GetListStrictByCodesForUpdateAsync(
        IReadOnlyCollection<string> codes,
        CancellationToken ct = default)
    {
        var result = await LockByCodes(codes)
            .ForUpdate()
            .BuildQuery()
            .ToListAsync(ct);

        var missing = codes.Except(result.Select(e => e.Code)).ToList();
        if (missing.Count > 0)
        {
            throw new DomErrResourceNotFound<T>(missing);
        }

        return result;
    }

    public virtual async Task<IReadOnlyList<T>> GetListStrictByCodesForShareAsync(IReadOnlyCollection<string> codes,
        CancellationToken ct = default)
    {
        var result = await LockByCodes(codes)
            .ForShare()
            .BuildQuery()
            .ToListAsync(ct);

        var missing = codes.Except(result.Select(e => e.Code)).ToList();
        if (missing.Count > 0)
        {
            throw new DomErrResourceNotFound<T>(missing);
        }

        return result;
    }

    protected PessimisticLockQueryBuilder<T> LockByCodes(IReadOnlyCollection<string> codes)
        => Lock().WhereIn(x => x.Code, codes);
}