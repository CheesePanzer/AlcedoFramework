namespace Alcedo.Framework.Admin.Application;

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