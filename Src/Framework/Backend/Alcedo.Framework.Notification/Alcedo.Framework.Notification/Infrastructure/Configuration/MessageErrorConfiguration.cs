using Alcedo.Core.Infrastructure.Configuration;
using Alcedo.Framework.Notification.Domain.Entity;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Alcedo.Framework.Notification.Infrastructure.Configuration;

internal class MessageErrorConfiguration : EntityConfigurationBase<MessageErrorEntity>
{
    protected override void ConfigureProperties(EntityTypeBuilder<MessageErrorEntity> builder)
    {
        builder.Property(e => e.PayloadJson).IsTextColumn();
        
        builder.Property(e => e.ErrorMsg).IsTextColumn();
    }
}