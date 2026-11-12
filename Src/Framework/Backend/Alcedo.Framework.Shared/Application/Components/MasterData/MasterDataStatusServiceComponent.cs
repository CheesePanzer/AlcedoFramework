using Alcedo.Core.Application.Tools;
using Alcedo.Core.Domain.DomException;
using Alcedo.Core.Domain.ReferenceIntegrity;
using Alcedo.Framework.Shared.Domain.Entity;
using Alcedo.Framework.Shared.Domain.IRepositoryBase;

namespace Alcedo.Framework.Shared.Application.Components.MasterData;

public class MasterDataStatusServiceComponent<TAgg>
    where TAgg : MasterDataAggregateRootBase
{
    protected readonly IMasterDataRepositoryBase<TAgg> Repository;

    protected readonly IUnitOfWorkExecutor Executor;

    protected readonly IAggRefChecker RefChecker;

    public MasterDataStatusServiceComponent(
        IMasterDataRepositoryBase<TAgg> repository,
        IUnitOfWorkExecutor executor, IAggRefChecker refChecker)
    {
        Repository = repository;
        Executor = executor;
        RefChecker = refChecker;
    }

    protected async Task MasterDataStatusOperateWithoutReferenceCheckAsync(
        Guid id,
        ContextOper oper,
        Action<TAgg, OperatorRecord> action, bool checkReference = false, CancellationToken ct = default)
    {
        if (checkReference)
        {
            await RefChecker.ThrowIfReferencedAsync<TAgg>(id, ct);
        }

        await Executor.ExecuteAsync(async () =>
        {
            var agg = await Repository.GetByIdForUpdateAsync(id, ct)
                      ?? throw new DomErrResourceNotFound<TAgg>(id);

            action(agg, OperatorRecord.From(oper));
        }, ct);
    }

    protected Task MasterDataStatusOperateWithReferenceCheckAsync(Guid id,
        ContextOper oper,
        Action<TAgg, OperatorRecord> action, CancellationToken ct = default)
        => MasterDataStatusOperateWithoutReferenceCheckAsync(id, oper, action, true, ct);

    public virtual Task EnableByIdAsync(Guid id, ContextOper oper, CancellationToken ct = default)
        => MasterDataStatusOperateWithoutReferenceCheckAsync(id, oper, (agg, oper1) => agg.Enable(oper1), ct: ct);

    public virtual Task DisableByIdAsync(Guid id, ContextOper oper, CancellationToken ct = default)
        => MasterDataStatusOperateWithReferenceCheckAsync(id, oper, (agg, oper1) => agg.Disable(oper1),
            ct: ct);
}