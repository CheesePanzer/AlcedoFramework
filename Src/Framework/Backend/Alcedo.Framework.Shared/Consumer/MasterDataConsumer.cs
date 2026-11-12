using Alcedo.Framework.Shared.Application;
using Alcedo.Framework.Shared.Application.Service;
using Alcedo.Framework.Shared.Domain.Entity;
using MassTransit;

namespace Alcedo.Framework.Shared.Consumer;

public class MasterDataAuditLogConsumerBase : IConsumer<MasterDataAuditLogEvent>
{
    private readonly IMasterDataLogWriteService _logService;

    public MasterDataAuditLogConsumerBase(IMasterDataLogWriteService logService)
    {
        _logService = logService;
    }

    public Task Consume(ConsumeContext<MasterDataAuditLogEvent> context)
        => _logService.SaveLogFromEvtAsync(context.Message);
}