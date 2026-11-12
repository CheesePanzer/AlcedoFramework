using Alcedo.Core.Infrastructure.Configuration;
using Alcedo.Framework.Notification.Domain.Entity;
using Alcedo.Framework.Shared.Infrastructure.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Alcedo.Framework.Notification.Infrastructure.Configuration;

internal class MessageTemplateConfiguration : MasterDataConfigurationBase<MessageTemplateAggRoot>
{
    protected override void ConfigureProperties(EntityTypeBuilder<MessageTemplateAggRoot> builder)
    {
        builder.Property(e => e.TitleTemplate).IsTextColumn();

        builder.Property(e => e.BodyTemplate).IsTextColumn();
    }
}