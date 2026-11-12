namespace Alcedo.Frontend.Abstraction.StateMachine;

#region StatusMachine

public sealed record MasterDataStateValue(string Value) : StateValue(Value);

public sealed record MasterDataStateTransitionTrigger(string Value) : StateTransitionTrigger(Value);

public class MasterDataStates
{
    public static readonly MasterDataStateValue Transient = new MasterDataStateValue("Transient");
    public static readonly MasterDataStateValue Draft = new MasterDataStateValue("Draft");
    public static readonly MasterDataStateValue Enabled = new MasterDataStateValue("Enabled");
    public static readonly MasterDataStateValue Disabled = new MasterDataStateValue("Disabled");
}

public class MasterDataStateTransitionTriggers
{
    public static readonly MasterDataStateTransitionTrigger Create = new MasterDataStateTransitionTrigger("Create");
    public static readonly MasterDataStateTransitionTrigger Enable = new MasterDataStateTransitionTrigger("Enable");
    public static readonly MasterDataStateTransitionTrigger Disable = new MasterDataStateTransitionTrigger("Disable");
}

public class MasterDataDefaultStatusFlow
    : StateFlowBase<MasterDataStateValue, MasterDataStateTransitionTrigger>
{
    public static readonly MasterDataDefaultStatusFlow Instance = new();

    public MasterDataDefaultStatusFlow()
    {
    }

    protected virtual IReadOnlyList<StateTransitionRule<MasterDataStateValue, MasterDataStateTransitionTrigger>> Base =>
    [
        new(MasterDataStates.Transient, MasterDataStateTransitionTriggers.Create,
            MasterDataStates.Draft),
        new(MasterDataStates.Draft, MasterDataStateTransitionTriggers.Enable,
            MasterDataStates.Enabled),
        new(MasterDataStates.Enabled, MasterDataStateTransitionTriggers.Disable,
            MasterDataStates.Disabled),
        new(MasterDataStates.Disabled, MasterDataStateTransitionTriggers.Enable,
            MasterDataStates.Enabled),
    ];

    protected virtual IReadOnlyList<StateTransitionRule<MasterDataStateValue, MasterDataStateTransitionTrigger>>
        Additional =>
        new List<StateTransitionRule<MasterDataStateValue, MasterDataStateTransitionTrigger>>();

    protected override IReadOnlyList<StateTransitionRule<MasterDataStateValue, MasterDataStateTransitionTrigger>>
        Transitions =>
        [.. Base, .. Additional];
}

#endregion

#region OperationGuard

public sealed record MasterDataStateAction(string Value) : StateAction(Value);

public class MasterDataStateActions
{
    public static readonly MasterDataStateAction Update = new MasterDataStateAction("Update");
    public static readonly MasterDataStateAction Delete = new MasterDataStateAction("Delete");
    public static readonly MasterDataStateAction Archive = new MasterDataStateAction("Archive");
}

public class MasterDataDefaultOperationGuard
    : StateActionGuardBase<MasterDataStateValue, MasterDataStateAction>
{
    protected virtual IReadOnlyDictionary<string, IReadOnlySet<MasterDataStateValue>> Base =>
        new Dictionary<string, IReadOnlySet<MasterDataStateValue>>
        {
            [MasterDataStateActions.Update.Value] = new HashSet<MasterDataStateValue>
                { MasterDataStates.Draft, MasterDataStates.Enabled, MasterDataStates.Disabled },
            [MasterDataStateActions.Delete.Value] = new HashSet<MasterDataStateValue>
                { MasterDataStates.Draft, MasterDataStates.Disabled },
        };

    protected virtual IReadOnlyDictionary<string, IReadOnlySet<MasterDataStateValue>> Additional =>
        new Dictionary<string, IReadOnlySet<MasterDataStateValue>>();

    protected override IReadOnlyDictionary<string, IReadOnlySet<MasterDataStateValue>> Rules =>
        Base.Concat(Additional)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
}

public class MasterDataArchiveOperationGuard
    : StateActionGuardBase<MasterDataStateValue, MasterDataStateAction>
{
    protected virtual IReadOnlyDictionary<string, IReadOnlySet<MasterDataStateValue>> Base =>
        new Dictionary<string, IReadOnlySet<MasterDataStateValue>>
        {
            [MasterDataStateActions.Update.Value] = new HashSet<MasterDataStateValue>
                { MasterDataStates.Draft, MasterDataStates.Enabled, MasterDataStates.Disabled },
            [MasterDataStateActions.Delete.Value] = new HashSet<MasterDataStateValue> { MasterDataStates.Draft },
            [MasterDataStateActions.Archive.Value] = new HashSet<MasterDataStateValue> { MasterDataStates.Disabled },
        };

    protected virtual IReadOnlyDictionary<string, IReadOnlySet<MasterDataStateValue>> Additional =>
        new Dictionary<string, IReadOnlySet<MasterDataStateValue>>();

    protected override IReadOnlyDictionary<string, IReadOnlySet<MasterDataStateValue>> Rules =>
        Base.Concat(Additional)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
}

#endregion