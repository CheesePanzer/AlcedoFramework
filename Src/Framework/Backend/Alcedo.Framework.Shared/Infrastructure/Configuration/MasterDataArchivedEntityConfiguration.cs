using Alcedo.Core.Infrastructure.Configuration;
using Alcedo.Framework.Shared.Domain.Entity;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Alcedo.Framework.Shared.Infrastructure.Configuration;

internal class MasterDataArchivedEntityTypeDefaultConfigurator<T> : EntityTypeDefaultConfigurator<T>
    where T : MasterDataArchivedEntityBase
{
    public override void Apply(EntityTypeBuilder<T> builder)
    {
        base.Apply(builder);
        builder.OwnsOne(ent => ent.Archiver);
    }
}

public class MasterDataArchivedEntityConfiguration<T> : EntityConfigurationBase<T>
where T : MasterDataArchivedEntityBase
{
    protected override IEntityTypeDefaultConfigurator<T> DefaultConfigurator =>
        new MasterDataArchivedEntityTypeDefaultConfigurator<T>();
}