using Alcedo.Core.Abstractions;
using Alcedo.Core.Application.Component;
using Alcedo.Core.Application.Tools;
using Alcedo.Core.Domain.IRepositoryBase;
using Alcedo.Framework.Shared.Application.Dto;
using Alcedo.Framework.Shared.Application.IQueryRepository;
using Alcedo.Framework.Shared.Application.SearchQuery;
using Alcedo.Framework.Shared.Domain.Entity;

namespace Alcedo.Framework.Shared.Application.Service;

public interface IMasterDataLogWriteService
{
    Task SaveLogFromEvtAsync(MasterDataAuditLogEvent evt, CancellationToken ct = default);
}

[RegisterAs(typeof(IMasterDataLogWriteService))]
public sealed class MasterDataLogWriteServiceBase : IMasterDataLogWriteService
{
    private readonly IEntityStoreBase<MasterDataAuditLogEntity> _repository;

    private readonly IUnitOfWorkExecutor _executor;

    public MasterDataLogWriteServiceBase(IEntityStoreBase<MasterDataAuditLogEntity> repository,
        IUnitOfWorkExecutor executor)
    {
        _repository = repository;
        _executor = executor;
    }

    public async Task SaveLogFromEvtAsync(MasterDataAuditLogEvent evt, CancellationToken ct = default)
    {
        await _executor.ExecuteAsync(() =>
        {
            var log = new MasterDataAuditLogEntity(evt.AggregateId, evt.AggregateRootIdentity, evt.StatusOperation,
                evt.FromStatus, evt.ToStatus,
                evt.Timestamp, evt.Note, evt.Operator);
            _repository.Add(log);
        }, ct);
    }
}

public sealed class
    MasterDataLogSearchService<TMasterData> : EntitySearchServiceComponent<MasterDataAuditLogEntity,
    MasterDataAuditLogDto, MasterDataLogSearchQuery>
    where TMasterData : MasterDataAggregateRootBase
{
    public MasterDataLogSearchService(
        IMasterDataAuditLogQueryRepository<TMasterData> repository) : base(repository)
    {
    }
}