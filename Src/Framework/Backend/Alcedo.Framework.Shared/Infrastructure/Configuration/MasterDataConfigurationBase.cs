using Alcedo.Core.Infrastructure;
using Alcedo.Core.Infrastructure.Configuration;
using Alcedo.Framework.Shared.Domain.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Alcedo.Framework.Shared.Infrastructure.Configuration;

internal class MasterDataTypeDefaultConfigurator<T> : ReferenceDataTypeDefaultConfigurator<T>
    where T : MasterDataAggregateRootBase
{
    public override void Apply(EntityTypeBuilder<T> builder)
    {
        base.Apply(builder);
        builder.Property(e => e.Status).IsValueAtom();
        builder.OwnsOne(o => o.Creator);
        builder.OwnsOne(o => o.Updater);
    }
}

public abstract class MasterDataConfigurationBase<T> : ReferenceDataConfigurationBase<T>
    where T : MasterDataAggregateRootBase
{
    protected override IEntityTypeDefaultConfigurator<T> DefaultConfigurator =>
        new MasterDataTypeDefaultConfigurator<T>();
}

public class MasterDataLogConfigurationBase : EntityConfigurationBase<MasterDataAuditLogEntity>
{
    protected override void ConfigureProperties(EntityTypeBuilder<MasterDataAuditLogEntity> builder)
    {
        builder.Property(e => e.Note)
            .HasColumnType("text");

        builder.OwnsOne(o => o.Operator);
    }

    protected override void ConfigureIndexes(EntityTypeBuilder<MasterDataAuditLogEntity> builder)
    {
        builder.HasIndex(e => new { e.AggregateId, e.AggregateIdentity });
    }
}