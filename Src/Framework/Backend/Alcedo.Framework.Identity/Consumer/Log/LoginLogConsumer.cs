using Alcedo.Framework.Identity.Application.Event;
using Alcedo.Framework.Identity.Application.Service.Log;
using MassTransit;

namespace Alcedo.Framework.Identity.Consumer.Log;

public class LoginLogConsumer : IConsumer<LoginLogEvent>
{
    private readonly ILoginLogApplicationService _service;

    public LoginLogConsumer(ILoginLogApplicationService loginLogApplicationService)
    {
        _service = loginLogApplicationService;
    }

    public Task Consume(ConsumeContext<LoginLogEvent> context)
        => _service.SaveLoginFromEvtLogAsync(context.Message);
}