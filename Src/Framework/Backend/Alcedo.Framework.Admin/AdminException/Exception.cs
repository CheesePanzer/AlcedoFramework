using Alcedo.Core.Abstractions;

namespace Alcedo.Framework.Admin.AdminException;

public sealed class AdminErrJobNotFound(string group, string name)
    : AlcedoBadRequestException($"Job {group}.{name} not found");

public sealed class AdminErrTriggerNotFound(string triggerGroup, string triggerName)
    : AlcedoBadRequestException($"Trigger {triggerGroup}.{triggerName} not found");

public sealed class AdminErrTriggerAlreadyExists(string triggerGroup, string triggerName)
    : AlcedoBadRequestException($"Trigger {triggerGroup}.{triggerName} already exists");

public sealed class AdminErrInvalidCronExpression()
    : AlcedoBadRequestException("Invalid cron expression");