using Alcedo.Core.Domain.Entity;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Alcedo.Core.Infrastructure.Configuration;

public class AggregateRootTypeDefaultConfigurator<T> : EntityTypeDefaultConfigurator<T>
    where T : AggregateRoot
{
    public override void Apply(EntityTypeBuilder<T> builder)
    {
        base.Apply(builder);
        builder.Property(e => e.Version).IsConcurrencyToken();
        builder.Ignore(e => e.ForceModified);
    }
}

public abstract class AggregateRootConfigurationBase<T> : EntityConfigurationBase<T>
    where T : AggregateRoot
{
    protected override IEntityTypeDefaultConfigurator<T>
        DefaultConfigurator => new AggregateRootTypeDefaultConfigurator<T>();
}