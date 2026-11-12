using Alcedo.Core.Application;

namespace Alcedo.Framework.Identity.Application.Event;

public sealed record LoginLogEvent(
    Guid? UserId,
    string LoginName,
    string IpAddress,
    bool IsSuccess,
    Guid? SessionId,
    string? FailReason,
    DateTimeOffset LoginAt) : ApplicationEventBase;