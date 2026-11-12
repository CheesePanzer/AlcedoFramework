using Alcedo.Core.Abstractions;
using Alcedo.Core.Infrastructure.EfCore;
using Alcedo.Framework.Identity.Domain.Entity;
using Alcedo.Framework.Identity.Domain.IRepository;
using Alcedo.Framework.Shared.Domain.Entity;
using Alcedo.Framework.Shared.Infrastructure.Repository;
using Microsoft.EntityFrameworkCore;

namespace Alcedo.Framework.Identity.Infrastructure.Repository;

[RegisterAs(typeof(IUserRepository))]
internal class UserRepository : MasterDataRepositoryBase<UserAgg>, IUserRepository
{
    public UserRepository(AlcedoDbContext context) : base(context)
    {
    }

    public async Task<bool> CheckIfLoginNameExistsAsync(string loginName, CancellationToken ct = default)
    {
        return await DbSet.AnyAsync(x => x.LoginName == loginName, cancellationToken: ct);
    }

    public async Task<UserAgg?> GetEnabledByLoginNameAsync(string loginName, CancellationToken ct = default)
    {
        return await DbSet
            .Where(a => a.LoginName == loginName &&
                        a.Status == MasterDataStates.Enabled)
            .SingleOrDefaultAsync(cancellationToken: ct);
    }

    public Task<UserAgg?> GetEnabledByLoginNameForUpdateAsync(string loginName, CancellationToken ct = default)
        =>
            Lock().Where(x => x.LoginName, loginName)
                .Where(x => x.Status, MasterDataStates.Enabled)
                .ForUpdate().BuildQuery()
                .SingleOrDefaultAsync(ct);

    public virtual async Task<UserAgg?> GetByIdWithRolesAsync(Guid id, CancellationToken ct = default)
    {
        return await DbSet
            .Where(b => b.Id == id)
            .Include(u => u.UserRoles)
            .AsNoTracking()
            .SingleOrDefaultAsync(cancellationToken: ct);
    }

    public async Task ArchiveHistoryUserAsync(HistoryUserEntity hUser, CancellationToken ct = default)
    {
        Context.RawEntity<HistoryUserEntity>().Add(hUser);
    }

    public async Task<bool> CheckIfAlreadyArchivedAsync(Guid userId, CancellationToken ct = default)
    {
        return await Context.RawEntity<HistoryUserEntity>().AnyAsync(hu => hu.Id == userId, ct);
    }
}