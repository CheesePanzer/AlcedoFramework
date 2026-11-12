using Alcedo.Core.Infrastructure.EfCore;
using Alcedo.Framework.Identity.Contracts;
using Alcedo.Framework.Identity.Domain.Entity;
using Alcedo.Framework.Shared;
using Alcedo.Framework.Shared.Domain.Entity;

namespace Alcedo.Framework.Identity.Infrastructure.Seeder;

public static class RootUserSeeder
{
    public static void Seed(AlcedoDbContext context)
    {
        if (context.AggRoot<UserAgg>().Any(u => u.IsRootUser))
        {
            return;
        }

        var admin = new UserAgg(
            "admin",
            "admin",
            "admin",
            PasswordHasher.Hash("admin"),
            "admin",
            null,
            null,
            new OperatorRecord(SystemUser.Id, SystemUser.Code, SystemUser.Name)
        );

        typeof(UserAgg)
            .GetProperty(nameof(admin.IsRootUser))?
            .SetValue(admin, true);

        typeof(UserAgg)
            .GetProperty(nameof(admin.Status))?
            .SetValue(admin, MasterDataStates.Enabled);
        typeof(UserAgg)
            .GetProperty(nameof(admin.RequiresPasswordChange))?
            .SetValue(admin, true);
        typeof(UserAgg)
            .GetProperty(nameof(admin.CreatedAt))?
            .SetValue(admin, DateTimeOffset.UtcNow);
        typeof(UserAgg)
            .GetProperty(nameof(admin.UpdatedAt))?
            .SetValue(admin, DateTimeOffset.UtcNow);

        context.AggRoot<UserAgg>().Add(admin);
        context.SaveChanges();
    }
}