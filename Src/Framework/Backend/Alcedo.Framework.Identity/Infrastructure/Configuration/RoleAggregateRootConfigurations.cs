using Alcedo.Core.Infrastructure.Configuration;
using Alcedo.Framework.Identity.Domain.Entity;
using Alcedo.Framework.Shared.Infrastructure.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Alcedo.Framework.Identity.Infrastructure.Configuration;

internal sealed class RoleAggregateConfiguration : MasterDataConfigurationBase<RoleAgg>
{
    protected override void ConfigureRelationships(EntityTypeBuilder<RoleAgg> builder)
    {
        builder.HasChildEntities(r => r.RolePermissions, rp => rp.RoleId);
    }
}

internal sealed class RolePermissionsEntityConfiguration : EntityConfigurationBase<RolePermissionsRelation>
{
    protected override void ConfigureIndexes(EntityTypeBuilder<RolePermissionsRelation> builder)
    {
        builder.HasIndex(u => new { u.RoleId, u.PermissionEndpoint }).IsUnique();
    }
}