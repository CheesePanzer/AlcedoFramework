using Alcedo.Core.Abstractions;
using Alcedo.Core.Application.Tools;
using Alcedo.Core.Domain.DomException;
using Alcedo.Framework.Notification.Domain.Entity;
using Alcedo.Framework.Shared;
using Alcedo.Framework.Shared.Domain.Entity;
using Alcedo.Framework.Shared.Domain.IRepositoryBase;

namespace Alcedo.Framework.Notification.Domain.Service;

[RegisterSelf]
internal sealed class MessageConfigDomainService
{
    private readonly IMasterDataRepositoryBase<MessageTemplateAggRoot> _templateRepo;

    private readonly IMasterDataRepositoryBase<MessageConfigAggRoot> _configRepo;

    private readonly IUnitOfWorkExecutor _executor;

    public MessageConfigDomainService(IMasterDataRepositoryBase<MessageTemplateAggRoot> templateRepo,
        IMasterDataRepositoryBase<MessageConfigAggRoot> configRepo, IUnitOfWorkExecutor executor)
    {
        _templateRepo = templateRepo;
        _configRepo = configRepo;
        _executor = executor;
    }

    public async Task BatchAssignMessageTemplatesAsync(Guid id, IReadOnlyCollection<Guid> messageTemplateIds,
        ContextOper oper,
        CancellationToken ct = default)
    {
        await _executor.ExecuteAsync(async () =>
        {
            var config = await _configRepo.GetByIdForUpdateAsync(id, ct) ??
                         throw new DomErrResourceNotFound<MessageConfigAggRoot>(id);

            var ents = await _templateRepo.GetEnabledListStrictByIdsForShareAsync(messageTemplateIds, ct);

            config.BatchAssignTemplates(ents.Select(ent => ent.Id).ToList(), OperatorRecord.From(oper));
        }, ct);
    }

    public async Task BatchRemoveMessageTemplatesAsync(Guid id, IReadOnlyCollection<Guid> messageTemplateIds,
        ContextOper oper,
        CancellationToken ct = default)
    {
        await _executor.ExecuteAsync(async () =>
        {
            var config = await _configRepo.GetByIdForUpdateAsync(id, ct) ??
                         throw new DomErrResourceNotFound<MessageConfigAggRoot>(id);

            config.BatchRemoveTemplates(messageTemplateIds, OperatorRecord.From(oper));
        }, ct);
    }

    public async Task ClearMessageTemplatesAsync(Guid id,
        ContextOper oper,
        CancellationToken ct = default)
    {
        await _executor.ExecuteAsync(async () =>
        {
            var config = await _configRepo.GetByIdForUpdateAsync(id, ct) ??
                         throw new DomErrResourceNotFound<MessageConfigAggRoot>(id);

            config.ClearTemplates(OperatorRecord.From(oper));
        }, ct);
    }
}