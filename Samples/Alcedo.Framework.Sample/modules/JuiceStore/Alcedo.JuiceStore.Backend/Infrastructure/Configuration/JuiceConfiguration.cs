using Alcedo.Core.Infrastructure.Configuration;
using Alcedo.Framework.Shared.Infrastructure.Configuration;
using Alcedo.JuiceStore.Backend.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Alcedo.JuiceStore.Backend.Infrastructure.Configuration;

internal sealed class JuiceConfiguration : MasterDataConfigurationBase<JuiceAggRoot>
{
    protected override void ConfigureProperties(EntityTypeBuilder<JuiceAggRoot> builder)
    {
        builder.Property(e => e.Name).HasMaxLength(200);
        builder.Property(e => e.Description).IsTextColumn();
        builder.Property(e => e.Flavor).HasMaxLength(100);
        builder.Property(e => e.Price).HasPrecision(18, 2);
        builder.Property(e => e.ImageUrl).HasMaxLength(2048);
    }

    protected override void ConfigureIndexes(EntityTypeBuilder<JuiceAggRoot> builder)
    {
        builder.HasIndex(e => e.Flavor);
        builder.HasIndex(e => new { e.IsFeatured, e.SortOrder });
    }
}
