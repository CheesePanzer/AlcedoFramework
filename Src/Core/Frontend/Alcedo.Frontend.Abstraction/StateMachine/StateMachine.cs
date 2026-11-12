namespace Alcedo.Frontend.Abstraction.StateMachine;

public abstract record StateValue(string Value) : ValueAtom(Value);

public abstract record StateTransitionTrigger(string Value) : ValueAtom(Value);

public record StateTransitionRule<TState, TTrigger>(
    TState From,
    TTrigger Operation,
    TState To
) where TState : StateValue
    where TTrigger : StateTransitionTrigger;

public abstract class StateFlowBase<TState, TTrigger>
    where TState : StateValue
    where TTrigger : StateTransitionTrigger
{
    private readonly Lazy<Dictionary<(TState From, TTrigger Op), TState>> _nextStateCache;
    private readonly Lazy<Dictionary<TState, List<TTrigger>>> _allowedOpsCache;

    protected StateFlowBase()
    {
        _nextStateCache = new Lazy<Dictionary<(TState, TTrigger), TState>>(() =>
            Transitions.ToDictionary(t => (t.From, t.Operation), t => t.To));

        _allowedOpsCache = new Lazy<Dictionary<TState, List<TTrigger>>>(() =>
            Transitions.GroupBy(t => t.From)
                .ToDictionary(g => g.Key, g => g.Select(t => t.Operation).ToList()));
    }

    protected abstract IReadOnlyList<StateTransitionRule<TState, TTrigger>> Transitions { get; }

    public bool CheckOperationValidity(TState status, TTrigger operation)
        => _nextStateCache.Value.ContainsKey((status, operation));

    public TState? GetNextState(TState status, TTrigger operation)
        => _nextStateCache.Value.GetValueOrDefault((status, operation));

    public IReadOnlyList<TTrigger> GetAllowedOperations(TState status)
        => _allowedOpsCache.Value.TryGetValue(status, out var operations)
            ? operations
            : Array.Empty<TTrigger>();
}

public abstract record StateAction(string Value) : ValueAtom(Value);

public abstract class StateActionGuardBase<TState, TAction>
    where TState : StateValue
    where TAction : StateAction
{
    protected virtual IReadOnlyDictionary<string, IReadOnlySet<TState>> Rules
        => new Dictionary<string, IReadOnlySet<TState>>();

    public bool IsAllowed(TState currentState, TAction action)
        => Rules.TryGetValue(action.Value, out var allowed) && allowed.Contains(currentState);
}