using Alcedo.Core.Abstractions;
using Alcedo.Core.Application.Tools;
using Alcedo.Core.Domain.ReferenceIntegrity;
using Alcedo.Framework.Notification.Application.Command;
using Alcedo.Framework.Notification.Domain.Entity;
using Alcedo.Framework.Shared.Application.Components.MasterData;
using Alcedo.Framework.Shared.Domain.Entity;
using Alcedo.Framework.Shared.Domain.IRepositoryBase;
using Microsoft.Extensions.Logging;

namespace Alcedo.Framework.Notification.Application.Service.MessageTemplate;

[RegisterSelf]
internal sealed class
    MessageTemplateEditServiceComponent : MasterDataEditServiceComponent<MessageTemplateAggRoot,
    MessageTemplateCreateCommand, MessageTemplateUpdateCommand>
{
    public MessageTemplateEditServiceComponent(
        IMasterDataRepositoryBase<MessageTemplateAggRoot> repository,
        ILoggerFactory loggerFactory,
        IAggRefChecker entityRefRelationCheckManager,
        IUnitOfWorkExecutor executor)
        : base(repository,
            loggerFactory,
            entityRefRelationCheckManager, executor)
    {
    }

    protected override MessageTemplateAggRoot CreateAggregateInternal(MessageTemplateCreateCommand command,
        OperatorRecord oper,
        CancellationToken ct = default)
    {
        return new MessageTemplateAggRoot(command.Code, command.Name, command.Channel, command.TitleTemplate,
            command.BodyTemplate,
            command.ExternalTemplateId, oper);
    }

    protected override void UpdateAggregateInternal(MessageTemplateAggRoot aggToBeUpdated,
        MessageTemplateUpdateCommand command,
        OperatorRecord oper, CancellationToken ct = default)
    {
        aggToBeUpdated.Update(command.Name, command.Channel, command.TitleTemplate, command.BodyTemplate,
            command.ExternalTemplateId,
            oper);
    }
}