using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;
using Serilog.Context;

namespace Alcedo.Core.Infrastructure.Job;

public abstract class AlcedoJobBase : IJob
{
    protected readonly IServiceProvider ServiceProvider;
    protected readonly ILogger Logger;

    protected AlcedoJobBase(IServiceProvider serviceProvider, ILoggerFactory loggerFactory)
    {
        ServiceProvider = serviceProvider;
        Logger = loggerFactory.CreateLogger(GetType());
    }

    async Task IJob.Execute(IJobExecutionContext context)
    {
        var jobName = context.JobDetail.Key.Name;
        var jobData = context.MergedJobDataMap.ToDictionary(k => k.Key, v => v.Value);
        var ct = context.CancellationToken;

        using var activity = new Activity("Quartz.Job").Start();

        using var _ = LogContext.PushProperty("TraceId", activity.TraceId.ToString());

        Logger.LogInformation("JobStart: {JobName}", jobName);
        var sw = Stopwatch.StartNew();

        try
        {
            using var scope = ServiceProvider.CreateScope();
            await ExecuteAsync(jobName, jobData, scope.ServiceProvider, ct);
            Logger.LogInformation("JobComplete: {JobName} Duration={Ms}ms", jobName, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "JobFailed: {JobName} Duration={Ms}ms", jobName, sw.ElapsedMilliseconds);
            throw;
        }
    }

    protected abstract Task ExecuteAsync(
        string jobName,
        IDictionary<string, object> data,
        IServiceProvider sp,
        CancellationToken ct);
}