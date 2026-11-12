using Alcedo.Core.Infrastructure.Configuration;
using Alcedo.Framework.Admin.Domain.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Alcedo.Framework.Admin.Infrastructure.Configuration;

internal class AccessLogEntityConfiguration : EntityConfigurationBase<AccessLogEntity>
{
    protected override void ConfigureProperties(EntityTypeBuilder<AccessLogEntity> builder)
    {
        builder.Property(e => e.UserAgent).IsTextColumn();
        builder.Property(e => e.Url).IsTextColumn();
        builder.Property(e => e.Exception).IsTextColumn();
    }
}