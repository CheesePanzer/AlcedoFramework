using Alcedo.Core.Domain.IRepositoryBase;
using Alcedo.Framework.Shared.Domain.Entity;

namespace Alcedo.Framework.Shared.Domain.IRepositoryBase;

public interface IReferenceDataRepositoryBase<T> : IAggregateRootRepositoryBase<T>
    where T : ReferenceDataAggregateRootBase
{
    /// <summary>
    /// To validate the uniqueness of code
    /// </summary>
    /// <param name="code"></param>
    /// <returns></returns>
    Task<bool> CheckIfCodeExistsAsync(string code, CancellationToken ct = default);

    Task<T?> GetByCodeNoTrackingAsync(string code, CancellationToken ct = default);

    Task<T?> GetByCodeAsync(string code, CancellationToken ct = default);

    Task<T?> GetByCodeForUpdateAsync(string code,
        CancellationToken ct = default);

    Task<T?> GetByCodeForShareAsync(string code,
        CancellationToken ct = default);

    Task<IReadOnlyList<T>> GetListByCodesNoTrackingAsync(IReadOnlyCollection<string> codes,
        CancellationToken ct = default);

    Task<List<T>> GetListByCodesAsync(IReadOnlyCollection<string> codes,
        CancellationToken ct = default);

    Task<List<T>> GetListPartialByCodesForUpdateAsync(
        IReadOnlyCollection<string> codes,
        CancellationToken ct = default);

    Task<IReadOnlyList<T>> GetListPartialByCodesForShareAsync(
        IReadOnlyCollection<string> codes,
        CancellationToken ct = default);

    Task<List<T>> GetListStrictByCodesForUpdateAsync(
        IReadOnlyCollection<string> codes,
        CancellationToken ct = default);

    Task<IReadOnlyList<T>> GetListStrictByCodesForShareAsync(IReadOnlyCollection<string> codes,
        CancellationToken ct = default);
}