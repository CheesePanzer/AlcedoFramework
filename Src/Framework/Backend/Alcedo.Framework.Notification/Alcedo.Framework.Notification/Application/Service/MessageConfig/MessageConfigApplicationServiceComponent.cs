using Alcedo.Core.Abstractions;
using Alcedo.Core.Application.AppException;
using Alcedo.Core.Application.Tools;
using Alcedo.Core.Domain.ReferenceIntegrity;
using Alcedo.Framework.Notification.Application.Command;
using Alcedo.Framework.Notification.Contracts;
using Alcedo.Framework.Notification.Domain.Entity;
using Alcedo.Framework.Shared.Application.Components.MasterData;
using Alcedo.Framework.Shared.Domain.Entity;
using Alcedo.Framework.Shared.Domain.IRepositoryBase;
using Microsoft.Extensions.Logging;

namespace Alcedo.Framework.Notification.Application.Service.MessageConfig;

[RegisterSelf]
internal sealed class
    MessageConfigEditServiceComponent : MasterDataEditServiceComponent<MessageConfigAggRoot,
    MessageConfigCreateCommand, MessageConfigUpdateCommand>
{
    public MessageConfigEditServiceComponent(
        IMasterDataRepositoryBase<MessageConfigAggRoot> repository,
        ILoggerFactory loggerFactory,
        IAggRefChecker entityRefRelationCheckManager,
        IUnitOfWorkExecutor executor)
        : base(repository,
            loggerFactory,
            entityRefRelationCheckManager, executor)
    {
    }

    protected override async Task CheckBeforeCreateAsync(MessageConfigCreateCommand command,
        CancellationToken ct = default)
    {
        await base.CheckBeforeCreateAsync(command, ct);

        if (await Repository.ExistsAsync(x => x.MessageTypeCode == command.MessageTypeCode, ct: ct))
        {
            throw new AppErrGeneric($"Message Code {command.MessageTypeCode} Already Has a Config");
        }
    }

    protected override MessageConfigAggRoot CreateAggregateInternal(MessageConfigCreateCommand command,
        OperatorRecord oper,
        CancellationToken ct = default)
    {
        if (!MessageTypeMetadataCenter.IfCodeExists(command.MessageTypeCode))
        {
            throw new AppErrGeneric($"Message Type Code {command.MessageTypeCode} Not Found");
        }

        return new MessageConfigAggRoot(command.Code, command.Name, command.MessageTypeCode, oper);
    }

    protected override async Task CheckBeforeUpdateAsync(Guid id, MessageConfigUpdateCommand command,
        CancellationToken ct = default)
    {
        await base.CheckBeforeUpdateAsync(id, command, ct);

        if (await Repository.ExistsAsync(x => x.MessageTypeCode == command.MessageTypeCode && x.Id != id, ct: ct))
        {
            throw new AppErrGeneric($"Message Code {command.MessageTypeCode} Already Has a Config");
        }
    }

    protected override void UpdateAggregateInternal(MessageConfigAggRoot aggToBeUpdated,
        MessageConfigUpdateCommand command,
        OperatorRecord oper, CancellationToken ct = default)
    {
        if (!MessageTypeMetadataCenter.IfCodeExists(command.MessageTypeCode))
        {
            throw new AppErrGeneric($"Message Type Code {command.MessageTypeCode} Not Found");
        }

        aggToBeUpdated.Update(command.Name, command.MessageTypeCode, oper);
    }
}