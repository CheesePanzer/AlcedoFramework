using Alcedo.Core.Abstractions;
using Alcedo.Framework.Identity.Infrastructure;
using Microsoft.Extensions.Options;

namespace Alcedo.Framework.Identity.Application.Factory;

internal sealed class LoginThrottleDecision
{
    public LoginThrottleKind Kind { get; }
    public int LockSeconds { get; }

    private LoginThrottleDecision(LoginThrottleKind kind, int lockSeconds = 0)
    {
        Kind = kind;
        LockSeconds = lockSeconds;
    }

    public static LoginThrottleDecision Allow() => new(LoginThrottleKind.Allow);
    public static LoginThrottleDecision TemporaryLock(int seconds) => new(LoginThrottleKind.TemporaryLock, seconds);
    public static LoginThrottleDecision PermanentLock() => new(LoginThrottleKind.PermanentLock);

    public bool IsBlocked => Kind != LoginThrottleKind.Allow;
}

internal enum LoginThrottleKind
{
    Allow,
    TemporaryLock,
    PermanentLock,
}

[RegisterSelf]
internal sealed class LoginThrottleDecisionFactory
{
    private readonly int _accountLockThreshold;

    public LoginThrottleDecisionFactory(
        IOptions<SecurityOptions> options)
    {
        var option = options.Value;

        _accountLockThreshold = option.AccountLockThresholdCount;
    }

    public LoginThrottleDecision Evaluate(long currentFailureCount)
    {
        if (currentFailureCount >= _accountLockThreshold)
        {
            return LoginThrottleDecision.PermanentLock();
        }

        return LoginThrottleDecision.Allow();
    }
}