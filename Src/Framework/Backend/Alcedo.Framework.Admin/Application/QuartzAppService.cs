using Alcedo.Core.Abstractions;
using Alcedo.Framework.Admin.AdminException;
using Quartz;
using Quartz.Impl.Matchers;

namespace Alcedo.Framework.Admin.Application;

public interface IQuartzAppService
{
    Task<List<JobDto>> GetJobsAsync();
    Task TriggerJobAsync(string group, string name);
    Task PauseJobAsync(string group, string name);
    Task ResumeJobAsync(string group, string name);
    Task AddTriggerAsync(string group, string name, AddTriggerRequest request);
    Task PauseTriggerAsync(string group, string name, string triggerGroup, string triggerName);
    Task ResumeTriggerAsync(string group, string name, string triggerGroup, string triggerName);
    Task PauseAllAsync();
    Task ResumeAllAsync();
    Task<SchedulerStatusDto> GetStatusAsync();
    Task UpdateCronAsync(string group, string name, string triggerGroup, string triggerName, string cronExpression);

    Task DeleteJobAsync(string group, string name);

    Task DeleteTriggerAsync(string group, string name, string triggerGroup, string triggerName);
}

[RegisterAs(typeof(IQuartzAppService))]
internal sealed class QuartzAppService : IQuartzAppService
{
    private readonly ISchedulerFactory _schedulerFactory;

    public QuartzAppService(ISchedulerFactory schedulerFactory)
        => _schedulerFactory = schedulerFactory;

    public async Task<List<JobDto>> GetJobsAsync()
    {
        var scheduler = await _schedulerFactory.GetScheduler();
        var jobGroups = await scheduler.GetJobGroupNames();

        var jobs = new List<JobDto>();
        foreach (var group in jobGroups)
        {
            var matcher = GroupMatcher<JobKey>.GroupEquals(group);
            var keys = await scheduler.GetJobKeys(matcher);
            foreach (var key in keys)
            {
                var detail = await scheduler.GetJobDetail(key);
                var triggers = await scheduler.GetTriggersOfJob(key);

                var triggerDtos = new List<TriggerDto>();
                foreach (var t in triggers)
                {
                    var state = await scheduler.GetTriggerState(t.Key);
                    triggerDtos.Add(new TriggerDto(
                        t.Key.Name,
                        t.Key.Group,
                        t.GetNextFireTimeUtc()?.UtcDateTime,
                        t.GetPreviousFireTimeUtc()?.UtcDateTime,
                        state.ToString()
                    ));
                }

                jobs.Add(new JobDto(key.Name, key.Group, detail?.Description, triggerDtos));
            }
        }

        return jobs;
    }

    public async Task TriggerJobAsync(string group, string name)
    {
        var scheduler = await _schedulerFactory.GetScheduler();
        var key = new JobKey(name, group);

        if (!await scheduler.CheckExists(key))
        {
            throw new AdminErrJobNotFound(group, name);
        }

        await scheduler.TriggerJob(key);
    }

    public async Task PauseJobAsync(string group, string name)
    {
        var scheduler = await _schedulerFactory.GetScheduler();
        var key = new JobKey(name, group);

        if (!await scheduler.CheckExists(key))
        {
            throw new AdminErrJobNotFound(group, name);
        }

        await scheduler.PauseJob(key);
    }

    public async Task ResumeJobAsync(string group, string name)
    {
        var scheduler = await _schedulerFactory.GetScheduler();
        var key = new JobKey(name, group);

        if (!await scheduler.CheckExists(key))
        {
            throw new AdminErrJobNotFound(group, name);
        }

        await scheduler.ResumeJob(key);
    }

    public async Task AddTriggerAsync(string group, string name, AddTriggerRequest request)
    {
        try
        {
            new CronExpression(request.CronExpression);
        }
        catch (FormatException)
        {
            throw new AdminErrInvalidCronExpression();
        }

        var scheduler = await _schedulerFactory.GetScheduler();
        var jobKey = new JobKey(name, group);

        if (!await scheduler.CheckExists(jobKey))
        {
            throw new AdminErrJobNotFound(group, name);
        }

        var triggerKey = new TriggerKey(request.TriggerName, request.TriggerGroup ?? group);

        if (await scheduler.CheckExists(triggerKey))
        {
            throw new AdminErrTriggerAlreadyExists(triggerKey.Group, triggerKey.Name);
        }

        var trigger = TriggerBuilder.Create()
            .WithIdentity(triggerKey)
            .ForJob(jobKey)
            .WithCronSchedule(request.CronExpression)
            .StartNow()
            .Build();

        await scheduler.ScheduleJob(trigger);
    }

    public async Task PauseTriggerAsync(string group, string name, string triggerGroup, string triggerName)
    {
        var scheduler = await _schedulerFactory.GetScheduler();
        var jobKey = new JobKey(name, group);

        if (!await scheduler.CheckExists(jobKey))
        {
            throw new AdminErrJobNotFound(group, name);
        }

        var triggerKey = new TriggerKey(triggerName, triggerGroup);

        if (!await scheduler.CheckExists(triggerKey))
        {
            throw new AdminErrTriggerNotFound(triggerGroup, triggerName);
        }

        await scheduler.PauseTrigger(triggerKey);
    }

    public async Task ResumeTriggerAsync(string group, string name, string triggerGroup, string triggerName)
    {
        var scheduler = await _schedulerFactory.GetScheduler();
        var jobKey = new JobKey(name, group);

        if (!await scheduler.CheckExists(jobKey))
        {
            throw new AdminErrJobNotFound(group, name);
        }

        var triggerKey = new TriggerKey(triggerName, triggerGroup);

        if (!await scheduler.CheckExists(triggerKey))
        {
            throw new AdminErrTriggerNotFound(triggerGroup, triggerName);
        }

        await scheduler.ResumeTrigger(triggerKey);
    }

    public async Task PauseAllAsync()
    {
        var scheduler = await _schedulerFactory.GetScheduler();
        await scheduler.PauseAll();
    }

    public async Task ResumeAllAsync()
    {
        var scheduler = await _schedulerFactory.GetScheduler();
        await scheduler.ResumeAll();
    }

    public async Task<SchedulerStatusDto> GetStatusAsync()
    {
        var scheduler = await _schedulerFactory.GetScheduler();
        var meta = await scheduler.GetMetaData();
        return new SchedulerStatusDto(
            scheduler.SchedulerName,
            scheduler.IsStarted,
            scheduler.InStandbyMode,
            scheduler.IsShutdown,
            meta.RunningSince?.UtcDateTime
        );
    }

    public async Task UpdateCronAsync(string group, string name, string triggerGroup, string triggerName,
        string cronExpression)
    {
        var scheduler = await _schedulerFactory.GetScheduler();
        var jobKey = new JobKey(name, group);

        if (!await scheduler.CheckExists(jobKey))
        {
            throw new AdminErrJobNotFound(group, name);
        }

        var triggerKey = new TriggerKey(triggerName, triggerGroup);

        if (!await scheduler.CheckExists(triggerKey))
        {
            throw new AdminErrTriggerNotFound(triggerGroup, triggerName);
        }

        var newTrigger = TriggerBuilder.Create()
            .WithIdentity(triggerKey)
            .ForJob(jobKey)
            .WithCronSchedule(cronExpression)
            .StartNow()
            .Build();

        await scheduler.RescheduleJob(triggerKey, newTrigger);
    }

    public async Task DeleteJobAsync(string group, string name)
    {
        var scheduler = await _schedulerFactory.GetScheduler();
        var key = new JobKey(name, group);

        if (!await scheduler.CheckExists(key))
        {
            throw new AdminErrJobNotFound(group, name);
        }

        await scheduler.DeleteJob(key);
    }

    public async Task DeleteTriggerAsync(string group, string name, string triggerGroup, string triggerName)
    {
        var scheduler = await _schedulerFactory.GetScheduler();
        var jobKey = new JobKey(name, group);

        if (!await scheduler.CheckExists(jobKey))
        {
            throw new AdminErrJobNotFound(group, name);
        }

        var triggerKey = new TriggerKey(triggerName, triggerGroup);

        if (!await scheduler.CheckExists(triggerKey))
        {
            throw new AdminErrTriggerNotFound(triggerGroup, triggerName);
        }

        await scheduler.UnscheduleJob(triggerKey);
    }
}