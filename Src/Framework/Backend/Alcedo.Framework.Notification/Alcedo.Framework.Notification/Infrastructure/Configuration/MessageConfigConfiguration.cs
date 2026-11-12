using Alcedo.Core.Infrastructure.Configuration;
using Alcedo.Framework.Notification.Domain.Entity;
using Alcedo.Framework.Shared.Infrastructure.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Alcedo.Framework.Notification.Infrastructure.Configuration;

internal class MessageConfigConfiguration : MasterDataConfigurationBase<MessageConfigAggRoot>
{
    protected override void ConfigureRelationships(EntityTypeBuilder<MessageConfigAggRoot> builder)
    {
        builder.HasChildEntities(cfg => cfg.TemplatesRelations, tr => tr.MessageConfigId);
    }

    protected override void ConfigureIndexes(EntityTypeBuilder<MessageConfigAggRoot> builder)
    {
        builder.HasIndex(p => p.MessageTypeCode).IsUnique();
    }
}

internal sealed class
    MessageConfigTemplateRelationsEntityConfiguration : EntityConfigurationBase<MessageConfigTemplateRelationsEntity>
{
    protected override void ConfigureRelationships(EntityTypeBuilder<MessageConfigTemplateRelationsEntity> builder)
    {
        builder.HasOne<MessageTemplateAggRoot>()
            .WithMany()
            .HasForeignKey(ur => ur.MessageTemplateId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    protected override void ConfigureIndexes(EntityTypeBuilder<MessageConfigTemplateRelationsEntity> builder)
    {
        builder.HasIndex(e => new { e.MessageConfigId, e.MessageTemplateId }).IsUnique();
    }
}