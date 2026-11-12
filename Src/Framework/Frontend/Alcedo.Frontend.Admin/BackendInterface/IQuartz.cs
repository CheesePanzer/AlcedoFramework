using Alcedo.Frontend.Abstraction;

namespace Alcedo.Frontend.Admin.BackendInterface;

public sealed record JobDto(
    string Name,
    string Group,
    string? Description,
    List<TriggerDto> Triggers
);

public sealed record TriggerDto(
    string Name,
    string Group,
    DateTimeOffset? NextFireTime,
    DateTimeOffset? PreviousFireTime,
    string State
);

public sealed record SchedulerStatusDto(
    string Name,
    bool IsStarted,
    bool IsStandby,
    bool IsShutdown,
    DateTimeOffset? RunningSince
);

public sealed record AddTriggerRequest(string TriggerName, string CronExpression, string? TriggerGroup);

public sealed record UpdateCronRequest(string CronExpression);

[ApiClient(Area = "admin", Controller = "quartz", Version = 1)]
public interface IQuartzClient
{
    [HttpGet("jobs")]
    Task<List<JobDto>> GetJobsAsync(CancellationToken ct = default);

    [HttpPost("jobs/{group}/{name}/trigger")]
    Task TriggerJobAsync([ToRoute] string group, [ToRoute] string name, CancellationToken ct = default);

    [HttpPost("jobs/{group}/{name}/pause")]
    Task PauseJobAsync([ToRoute] string group, [ToRoute] string name, CancellationToken ct = default);

    [HttpPost("jobs/{group}/{name}/resume")]
    Task ResumeJobAsync([ToRoute] string group, [ToRoute] string name, CancellationToken ct = default);

    [HttpPost("jobs/{group}/{name}/triggers/{triggerGroup}/{triggerName}/pause")]
    Task PauseTriggerAsync([ToRoute] string group, [ToRoute] string name,
        [ToRoute] string triggerGroup, [ToRoute] string triggerName, CancellationToken ct = default);

    [HttpPost("jobs/{group}/{name}/triggers/{triggerGroup}/{triggerName}/resume")]
    Task ResumeTriggerAsync([ToRoute] string group, [ToRoute] string name,
        [ToRoute] string triggerGroup, [ToRoute] string triggerName, CancellationToken ct = default);

    [HttpPost("scheduler/pause")]
    Task PauseAllAsync(CancellationToken ct = default);

    [HttpPost("scheduler/resume")]
    Task ResumeAllAsync(CancellationToken ct = default);

    [HttpGet("scheduler/status")]
    Task<SchedulerStatusDto> GetStatusAsync(CancellationToken ct = default);

    [HttpPut("jobs/{group}/{name}/triggers/{triggerGroup}/{triggerName}/cron")]
    Task UpdateCronAsync([ToRoute] string group, [ToRoute] string name,
        [ToRoute] string triggerGroup, [ToRoute] string triggerName,
        [ToBody] UpdateCronRequest request, CancellationToken ct = default);

    [HttpDelete("jobs/{group}/{name}")]
    Task DeleteJobAsync([ToRoute] string group, [ToRoute] string name, CancellationToken ct = default);

    [HttpDelete("jobs/{group}/{name}/triggers/{triggerGroup}/{triggerName}")]
    Task DeleteTriggerAsync([ToRoute] string group, [ToRoute] string name,
        [ToRoute] string triggerGroup, [ToRoute] string triggerName, CancellationToken ct = default);
}