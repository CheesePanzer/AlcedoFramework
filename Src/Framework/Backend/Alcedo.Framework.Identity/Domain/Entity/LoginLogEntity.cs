using Alcedo.Core.Domain.Entity;

namespace Alcedo.Framework.Identity.Domain.Entity;

public sealed class LoginLogEntity : EntityBase
{
    public Guid? UserId { get; private set; }
    public string LoginName { get; private set; }
    public string IpAddress { get; private set; }
    public bool IsSuccess { get; private set; }
    public Guid? SessionId { get; private set; }
    public string? FailReason { get; private set; }
    public DateTimeOffset LoginAt { get; private set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    private LoginLogEntity()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    {
    }

    public LoginLogEntity(
        Guid? userId,
        string loginName,
        string ipAddress,
        bool isSuccess,
        Guid? sessionId,
        string? failReason,
        DateTimeOffset loginAt)
    {
        UserId = userId;
        LoginName = loginName;
        IpAddress = ipAddress;
        IsSuccess = isSuccess;
        FailReason = failReason;
        SessionId = sessionId;
        LoginAt = loginAt;
    }
}