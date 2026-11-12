using System.Reflection;
using Alcedo.Core.Infrastructure.InfraException;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace Alcedo.Core.Infrastructure.Job;

public static class QuartzExtensions
{
    public static IServiceCollection AddAutoQuartzJobs(this IServiceCollection services, IConfiguration configuration,
        IEnumerable<Assembly> assemblies)
    {
        var dataSource = configuration
            .GetSection(nameof(DataSourceOptions))
            .Get<DataSourceOptions>() ?? throw new InvalidOperationException("Missing DataSource in appsettings.json");

        var connectionString = dataSource.Postgres;

        services.AddQuartz(q =>
        {
            q.UsePersistentStore(s =>
            {
                s.UseProperties = true;
                s.UsePostgres(db =>
                {
                    db.ConnectionString = connectionString;
                    db.TablePrefix = "quartz.QRTZ_";
                });
                s.UseSystemTextJsonSerializer();
            });

            var jobTypes = assemblies.SelectMany(a => a.GetTypes())
                .Where(t => t.GetCustomAttribute<QuartzJobAttribute>() is not null && !t.IsAbstract);

            q.InterruptJobsOnShutdown = true;

            foreach (var type in jobTypes)
            {
                var attr = type.GetCustomAttribute<QuartzJobAttribute>() ??
                           throw new InfraErrSchedulerConfigMissing(type.Name);
                var jobName = attr.JobName ?? type.Name.Replace("Job", "");
                var jobGroup = attr.JobGroup ?? "Default";
                var jobKey = new JobKey(jobName, jobGroup);

                q.AddJob(type, jobKey, opts => opts.WithDescription(type.Name).StoreDurably());

                q.AddTrigger(opts =>
                {
                    opts.ForJob(jobKey).WithIdentity($"{jobName}-trigger", jobGroup);

                    if (!string.IsNullOrEmpty(attr.CronExpression))
                    {
                        opts.WithCronSchedule(attr.CronExpression);
                    }
                    else if (attr.IntervalInSeconds.HasValue && attr.IntervalInSeconds.Value > 0)
                    {
                        opts.WithSimpleSchedule(x => x
                            .WithIntervalInSeconds(attr.IntervalInSeconds.Value)
                            .RepeatForever());
                    }
                    else
                    {
                        throw new InfraErrSchedulerConfigInvalid(jobName);
                    }
                });
            }
        });

        services.Configure<QuartzOptions>(options =>
        {
            options.Scheduling.IgnoreDuplicates = true;

            options.Scheduling.OverWriteExistingData = false;
        });

        services.AddQuartzHostedService(q =>
        {
            q.WaitForJobsToComplete = true;
            q.AwaitApplicationStarted = true;
        });

        return services;
    }
}