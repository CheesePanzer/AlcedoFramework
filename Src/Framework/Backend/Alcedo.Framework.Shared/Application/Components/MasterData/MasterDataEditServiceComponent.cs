using Alcedo.Core.Application.Tools;
using Alcedo.Core.Domain.DomException;
using Alcedo.Core.Domain.ReferenceIntegrity;
using Alcedo.Framework.Shared.Application.Command;
using Alcedo.Framework.Shared.Domain.Entity;
using Alcedo.Framework.Shared.Domain.IRepositoryBase;
using Microsoft.Extensions.Logging;

namespace Alcedo.Framework.Shared.Application.Components.MasterData;

public abstract class
    MasterDataEditServiceComponent<TAgg, TCreateCommand, TUpdateCommand>
    where TAgg : MasterDataAggregateRootBase
    where TCreateCommand : MasterDataCreateCommandBase
    where TUpdateCommand : MasterDataUpdateCommandBase
{
    protected readonly IMasterDataRepositoryBase<TAgg> Repository;

    protected readonly IAggRefChecker EntityRefRelationCheckManager;

    protected readonly IUnitOfWorkExecutor Executor;

    public MasterDataEditServiceComponent(IMasterDataRepositoryBase<TAgg> repository,
        ILoggerFactory loggerFactory,
        IAggRefChecker entityRefRelationCheckManager,
        IUnitOfWorkExecutor executor)
    {
        Repository = repository;
        EntityRefRelationCheckManager = entityRefRelationCheckManager;
        Executor = executor;
    }

    public virtual async Task<Guid> CreateAsync(TCreateCommand command, ContextOper oper, CancellationToken ct = default)
    {
        await CheckBeforeCreateAsync(command, ct);

        return await Executor.ExecuteAsync(async () =>
        {
            var newAgg = CreateAggregateInternal(command, OperatorRecord.From(oper));

            Repository.Add(newAgg);
            
            await AfterCreateAsync(newAgg, ct);
            
            return newAgg.Id;
        }, ct);
    }

    protected virtual async Task CheckBeforeCreateAsync(TCreateCommand command, CancellationToken ct = default)
    {
        if (await Repository.CheckIfCodeExistsAsync(command.Code, ct))
        {
            throw new DomErrDuplicateEntry("Code");
        }
    }
    
    protected virtual Task AfterCreateAsync(TAgg newAgg, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    protected abstract TAgg CreateAggregateInternal(TCreateCommand command, OperatorRecord oper,
        CancellationToken ct = default);

    /// <summary>
    /// Update By Update Command
    /// </summary>
    /// <param name="id"></param>
    /// <param name="command"></param>
    /// <param name="operId"></param>
    /// <exception cref="BErrorAggNotFound{UserAggregate}"></exception>
    public virtual async Task UpdateByIdAsync(Guid id, TUpdateCommand command, ContextOper oper, CancellationToken ct = default)
    {
        await CheckBeforeUpdateAsync(id, command, ct);

        await Executor.ExecuteAsync(async () =>
        {
            var aggToBeUpdated = await Repository.GetByIdForUpdateAsync(id, ct) ??
                                 throw new DomErrResourceNotFound<TAgg>(id);

            if (aggToBeUpdated.Version != command.Version)
            {
                throw new DomErrAggVersionConflict<TAgg>(aggToBeUpdated.Version, command.Version);
            }

            UpdateAggregateInternal(aggToBeUpdated, command, OperatorRecord.From(oper), ct);

            return aggToBeUpdated;
        }, ct);
    }

    protected virtual Task CheckBeforeUpdateAsync(Guid id, TUpdateCommand command, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    protected abstract void UpdateAggregateInternal(TAgg aggToBeUpdated, TUpdateCommand command, OperatorRecord oper,
        CancellationToken ct = default);

    /// <summary>
    /// 删除聚合根
    /// </summary>
    /// <param name="id"></param>
    /// <param name="operId"></param>
    /// <exception cref="BErrorAggNotFound{TAgg}"></exception>
    public virtual async Task DeleteByIdAsync(
        Guid id,
        ContextOper oper,
        CancellationToken ct = default)
    {
        await Executor.ExecuteAsync(async () =>
        {
            var agg = await Repository.GetByIdForUpdateAsync(id, ct);

            if (agg is null)
            {
                return;
            }

            await EntityRefRelationCheckManager.ThrowIfReferencedAsync<TAgg>(id, ct);
            
            await CheckBeforeDeleteAsync(agg, ct);

            agg.Delete(OperatorRecord.From(oper));

            Repository.Remove(agg);
            
            await AfterDeleteAsync(agg, ct);
        }, ct);
    }
    
    protected virtual Task CheckBeforeDeleteAsync(TAgg aggToBeDeleted, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    protected virtual Task AfterDeleteAsync(TAgg removedAgg, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }
}