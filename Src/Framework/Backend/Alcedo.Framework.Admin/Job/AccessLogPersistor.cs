using Alcedo.Core.Infrastructure.Job;
using Alcedo.Framework.Admin.Application.AccessLog;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Alcedo.Framework.Admin.Job;

[Obsolete("Since Useing Serilog Request Logging this is of no use no")]
//[QuartzJob(cronExpression: "0 * * * * ?", "AccessLogPersistor", "Admin")]
public sealed class AccessLogRedisToDb : AlcedoJobBase
{
    public AccessLogRedisToDb(IServiceProvider serviceProvider, ILoggerFactory loggerFactory) : base(serviceProvider,
        loggerFactory)
    {
    }

    protected override async Task ExecuteAsync(string jobName, IDictionary<string, object> data, IServiceProvider sp,
        CancellationToken ct)
    {
        var application = ServiceProvider.GetRequiredService<IAccessLogApplicationService>();

        await application.FetchAndSaveAccessLogAsync(ct);
    }
}