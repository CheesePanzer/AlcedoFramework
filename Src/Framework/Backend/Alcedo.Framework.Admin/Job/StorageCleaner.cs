using Alcedo.Core.Infrastructure.Job;
using Alcedo.Core.Infrastructure.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Alcedo.Framework.Admin.Job;

[QuartzJob(cronExpression: "0 0 0 * * ?", "StorageCleaner", "Admin")]
public sealed class StorageCleaner : AlcedoJobBase
{
    public StorageCleaner(IServiceProvider serviceProvider, ILoggerFactory loggerFactory) : base(serviceProvider, loggerFactory)
    {
    }

    protected override async Task ExecuteAsync(string jobName, IDictionary<string, object> data, IServiceProvider sp, CancellationToken ct)
    {
        var application = ServiceProvider.GetRequiredService<IStorageGcExecutor>();

        await application.ExecuteAsync(cancellationToken: ct);
    }
}