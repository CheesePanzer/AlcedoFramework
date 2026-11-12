using Alcedo.Core.Infrastructure.Configuration;
using Alcedo.Framework.Shared.Domain.Entity;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Alcedo.Framework.Shared.Infrastructure.Configuration;

internal class ReferenceDataTypeDefaultConfigurator<T> : AggregateRootTypeDefaultConfigurator<T>
    where T : ReferenceDataAggregateRootBase
{
    public override void Apply(EntityTypeBuilder<T> builder)
    {
        base.Apply(builder);
        builder.HasIndex(e => e.Code).IsUnique();
    }
}

public abstract class ReferenceDataConfigurationBase<T> : AggregateRootConfigurationBase<T>
    where T : ReferenceDataAggregateRootBase
{
    protected override IEntityTypeDefaultConfigurator<T> DefaultConfigurator =>
        new ReferenceDataTypeDefaultConfigurator<T>();
}