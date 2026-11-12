using Alcedo.Core.Abstractions;
using Alcedo.Core.Application.Tools;
using Alcedo.Core.Domain.ReferenceIntegrity;
using Alcedo.Framework.Identity.Application.Command;
using Alcedo.Framework.Identity.Domain.Entity;
using Alcedo.Framework.Shared;
using Alcedo.Framework.Shared.Application.Components.MasterData;
using Alcedo.Framework.Shared.Domain.Entity;
using Alcedo.Framework.Shared.Domain.IRepositoryBase;
using Microsoft.Extensions.Logging;

namespace Alcedo.Framework.Identity.Application.Service.Role;

[RegisterSelf]
internal sealed class
    RoleEditServiceComponent(
        IMasterDataRepositoryBase<RoleAgg> repository,
        ILoggerFactory loggerFactory,
        IAggRefChecker entityRefRelationCheckManager,
        IUnitOfWorkExecutor executor)
    : MasterDataEditServiceComponent<RoleAgg, RoleCreateCommand, RoleUpdateCommand>(repository,
        loggerFactory,
        entityRefRelationCheckManager, executor)
{
    protected override RoleAgg CreateAggregateInternal(RoleCreateCommand command, OperatorRecord oper,
        CancellationToken ct = default)
    {
        return new RoleAgg(command.Code, command.Name, command.Description, oper);
    }

    protected override void UpdateAggregateInternal(RoleAgg toBeUpdated, RoleUpdateCommand command,
        OperatorRecord oper, CancellationToken ct = default)
    {
        toBeUpdated.Update(command.Name, command.Description, oper);
    }
}

[RegisterSelf]
internal sealed class
    RoleStatusServiceComponent : MasterDataStatusServiceComponent<RoleAgg>
{
    public RoleStatusServiceComponent(IMasterDataRepositoryBase<RoleAgg> repository, IUnitOfWorkExecutor executor,
        IAggRefChecker refChecker) : base(
        repository, executor, refChecker)
    {
    }

    public override Task DisableByIdAsync(Guid id, ContextOper oper, CancellationToken ct = default)
        => MasterDataStatusOperateWithoutReferenceCheckAsync(id, oper, (agg, oper2) => agg.Disable(oper2), ct: ct);
}