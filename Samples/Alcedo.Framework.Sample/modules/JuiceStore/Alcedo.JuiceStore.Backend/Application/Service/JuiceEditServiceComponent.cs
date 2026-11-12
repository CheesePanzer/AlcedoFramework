using Alcedo.Core.Abstractions;
using Alcedo.Core.Application.Tools;
using Alcedo.Core.Domain.ReferenceIntegrity;
using Alcedo.Framework.Shared.Application.Components.MasterData;
using Alcedo.Framework.Shared.Domain.Entity;
using Alcedo.Framework.Shared.Domain.IRepositoryBase;
using Alcedo.JuiceStore.Backend.Application.Command;
using Alcedo.JuiceStore.Backend.Domain;
using Microsoft.Extensions.Logging;

namespace Alcedo.JuiceStore.Backend.Application.Service;

[RegisterSelf]
internal sealed class JuiceEditServiceComponent : MasterDataEditServiceComponent<JuiceAggRoot,
    JuiceCreateCommand, JuiceUpdateCommand>
{
    public JuiceEditServiceComponent(
        IMasterDataRepositoryBase<JuiceAggRoot> repository,
        ILoggerFactory loggerFactory,
        IAggRefChecker entityRefRelationCheckManager,
        IUnitOfWorkExecutor executor)
        : base(repository, loggerFactory, entityRefRelationCheckManager, executor)
    {
    }

    protected override JuiceAggRoot CreateAggregateInternal(JuiceCreateCommand command, OperatorRecord oper,
        CancellationToken ct = default) =>
        new(command.Code, command.Name, command.Description, command.Flavor, command.VolumeMl, command.Price,
            command.ImageUrl, command.IsFeatured, command.SortOrder, oper);

    protected override void UpdateAggregateInternal(JuiceAggRoot aggToBeUpdated, JuiceUpdateCommand command,
        OperatorRecord oper, CancellationToken ct = default) =>
        aggToBeUpdated.Update(command.Name, command.Description, command.Flavor, command.VolumeMl, command.Price,
            command.ImageUrl, command.IsFeatured, command.SortOrder, oper);
}
