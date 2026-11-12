using Alcedo.Framework.Identity.Application.Factory;
using Alcedo.Framework.Identity.Infrastructure;
using Microsoft.Extensions.Options;

namespace Alcedo.Framework.Identity.Test.Application.Providers;

public class LoginThrottleDecisionFactoryTests
{
    private static LoginThrottleDecisionFactory CreateFactory(int threshold = 5)
    {
        var options = Options.Create(new SecurityOptions { AccountLockThresholdCount = threshold });
        return new LoginThrottleDecisionFactory(options);
    }

    [Fact]
    public void Evaluate_ZeroFailures_Allows()
    {
        var factory = CreateFactory(5);
        var decision = factory.Evaluate(0);
        Assert.False(decision.IsBlocked);
    }

    [Fact]
    public void Evaluate_JustBelowThreshold_Allows()
    {
        var factory = CreateFactory(5);
        var decision = factory.Evaluate(4);
        Assert.False(decision.IsBlocked);
    }

    [Fact]
    public void Evaluate_AtThreshold_PermanentLock()
    {
        var factory = CreateFactory(5);
        var decision = factory.Evaluate(5);
        Assert.True(decision.IsBlocked);
    }

    [Fact]
    public void Evaluate_AboveThreshold_PermanentLock()
    {
        var factory = CreateFactory(5);
        var decision = factory.Evaluate(100);
        Assert.True(decision.IsBlocked);
    }

    [Fact]
    public void Evaluate_CustomThreshold_RespectsValue()
    {
        var factory = CreateFactory(3);
        var decision = factory.Evaluate(3);
        Assert.True(decision.IsBlocked);
    }
}
