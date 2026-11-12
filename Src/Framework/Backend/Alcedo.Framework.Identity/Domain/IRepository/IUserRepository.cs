using Alcedo.Framework.Identity.Domain.Entity;
using Alcedo.Framework.Shared.Domain.IRepositoryBase;

namespace Alcedo.Framework.Identity.Domain.IRepository;

public interface IUserRepository : IMasterDataRepositoryBase<UserAgg>
{
    /// <summary>
    /// If Login Name is already in use
    /// </summary>
    /// <param name="loginName"></param>
    /// <returns></returns>
    Task<bool> CheckIfLoginNameExistsAsync(string loginName, CancellationToken ct = default);

    /// <summary>
    /// Searches for an entity by Id and modifies it using optimistic concurrency.
    /// </summary>
    /// <param name="id">Entity ID</param>
    /// <returns>The entity or null</returns>
    Task<UserAgg?> GetByIdWithRolesAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Gets an enabled user by login name for authentication.
    /// </summary>
    /// <param name="loginName">Login name</param>
    /// <returns>The entity or null</returns>
    Task<UserAgg?> GetEnabledByLoginNameAsync(string loginName, CancellationToken ct = default);

    /// <summary>
    /// Gets an enabled user by login name for authentication (For Update).
    /// </summary>
    /// <param name="loginName">Login name</param>
    /// <returns>The entity or null</returns>
    Task<UserAgg?> GetEnabledByLoginNameForUpdateAsync(string loginName, CancellationToken ct = default);

    /// <summary>
    /// Archives a historical user record.
    /// </summary>
    /// <param name="hUser">Historical user entity</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task ArchiveHistoryUserAsync(HistoryUserEntity hUser, CancellationToken ct = default);

    /// <summary>
    /// Check Whether a guid is already archived
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task<bool> CheckIfAlreadyArchivedAsync(Guid userId, CancellationToken ct = default);
}