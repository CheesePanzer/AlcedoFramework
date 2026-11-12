using Alcedo.Core.Infrastructure.Configuration;
using Alcedo.Core.Infrastructure.EfCore;
using Alcedo.Framework.Identity.Domain.Entity;
using Alcedo.Framework.Shared.Infrastructure.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Alcedo.Framework.Identity.Infrastructure.Configuration;

internal sealed class UserAggregateConfiguration : MasterDataConfigurationBase<UserAgg>
{
    protected override void ConfigureIndexes(EntityTypeBuilder<UserAgg> builder)
    {
        builder.HasIndex(u => u.LoginName)
            .IsUnique();

        builder.HasIndex(u => u.IsRootUser)
            .IsUnique()
            .HasFilter("is_root_user = true")
            .HasDatabaseName($"IX_{nameof(UserAgg)}_Root_OnlyOne");
    }

    protected override void ConfigureRelationships(EntityTypeBuilder<UserAgg> builder)
    {
        builder.HasChildEntities(u => u.UserRoles, ur => ur.UserId);
    }

    protected override void ConfigureConverters(EntityTypeBuilder<UserAgg> builder)
    {
        builder.Property(u => u.TwoFactorSecret)
            .HasConversion(AlcedoColumnEncryptor.Converter);
    }
}

internal sealed class UserRolesEntityConfiguration : EntityConfigurationBase<UserRolesRelation>
{
    protected override void ConfigureRelationships(EntityTypeBuilder<UserRolesRelation> builder)
    {
        builder.HasOne<RoleAgg>()
            .WithMany()
            .HasForeignKey(ur => ur.RoleId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    protected override void ConfigureIndexes(EntityTypeBuilder<UserRolesRelation> builder)
    {
        builder.HasIndex(e => new { e.UserId, e.RoleId }).IsUnique();
    }
}

internal sealed class HistoryUserEntityConfiguration : EntityConfigurationBase<HistoryUserEntity>
{
    protected override void ConfigureRelationships(EntityTypeBuilder<HistoryUserEntity> builder)
    {
        builder.OwnsOne(e => e.Operator);
    }
}