namespace Alcedo.Core.Domain.Test;

public sealed record TestState(string Value) : StateValue(Value);

public sealed record TestTrigger(string Value) : StateTransitionTrigger(Value);

public class TestStateFlow : StateFlowBase<TestState, TestTrigger>
{
    public static readonly TestState Draft = new("Draft");
    public static readonly TestState Pending = new("Pending");
    public static readonly TestState Approved = new("Approved");
    public static readonly TestState Rejected = new("Rejected");
    public static readonly TestState Cancelled = new("Cancelled");

    public static readonly TestTrigger Submit = new("Submit");
    public static readonly TestTrigger Approve = new("Approve");
    public static readonly TestTrigger Reject = new("Reject");
    public static readonly TestTrigger Cancel = new("Cancel");
    public static readonly TestTrigger Revise = new("Revise");

    protected override IReadOnlyList<StateTransitionRule<TestState, TestTrigger>> Transitions { get; } =
    [
        new(Draft, Submit, Pending),
        new(Pending, Approve, Approved),
        new(Pending, Reject, Rejected),
        new(Pending, Cancel, Cancelled),
        new(Rejected, Revise, Draft)
    ];
}

public class StateMachineTests
{
    private readonly TestStateFlow _flow = new();

    [Theory]
    [InlineData("Draft", "Submit", true)]
    [InlineData("Draft", "Approve", false)]
    [InlineData("Draft", "Reject", false)]
    [InlineData("Pending", "Approve", true)]
    [InlineData("Pending", "Reject", true)]
    [InlineData("Pending", "Cancel", true)]
    [InlineData("Pending", "Submit", false)]
    [InlineData("Approved", "Approve", false)]
    [InlineData("Approved", "Cancel", false)]
    [InlineData("Rejected", "Revise", true)]
    [InlineData("Cancelled", "Submit", false)]
    public void CheckOperationValidity_ReturnsExpected(string stateValue, string triggerValue, bool expected)
    {
        var state = new TestState(stateValue);
        var trigger = new TestTrigger(triggerValue);

        var result = _flow.CheckOperationValidity(state, trigger);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetNextState_ValidTransition_ReturnsTargetState()
    {
        var result = _flow.GetNextState(TestStateFlow.Draft, TestStateFlow.Submit);

        Assert.Equal(TestStateFlow.Pending, result);
    }

    [Fact]
    public void GetNextState_InvalidTransition_ReturnsNull()
    {
        var result = _flow.GetNextState(TestStateFlow.Approved, TestStateFlow.Submit);

        Assert.Null(result);
    }

    [Fact]
    public void GetNextState_RejectedToDraft_ReturnsDraft()
    {
        var result = _flow.GetNextState(TestStateFlow.Rejected, TestStateFlow.Revise);

        Assert.Equal(TestStateFlow.Draft, result);
    }

    [Fact]
    public void GetAllowedOperations_Draft_ReturnsOnlySubmit()
    {
        var ops = _flow.GetAllowedOperations(TestStateFlow.Draft);

        Assert.Single(ops);
        Assert.Contains(TestStateFlow.Submit, ops);
    }

    [Fact]
    public void GetAllowedOperations_Pending_ReturnsApproveRejectCancel()
    {
        var ops = _flow.GetAllowedOperations(TestStateFlow.Pending);

        Assert.Equal(3, ops.Count);
        Assert.Contains(TestStateFlow.Approve, ops);
        Assert.Contains(TestStateFlow.Reject, ops);
        Assert.Contains(TestStateFlow.Cancel, ops);
    }

    [Fact]
    public void GetAllowedOperations_Approved_ReturnsEmpty()
    {
        var ops = _flow.GetAllowedOperations(TestStateFlow.Approved);

        Assert.Empty(ops);
    }

    [Fact]
    public void GetAllowedOperations_UnknownState_ReturnsEmpty()
    {
        var unknown = new TestState("Unknown");

        var ops = _flow.GetAllowedOperations(unknown);

        Assert.Empty(ops);
    }

    [Fact]
    public void StateValue_Equality_Works()
    {
        var a = new TestState("Draft");
        var b = new TestState("Draft");
        var c = new TestState("Pending");

        Assert.Equal(a, b);
        Assert.NotEqual(a, c);
    }
}