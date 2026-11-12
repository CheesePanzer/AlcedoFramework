using Alcedo.Core.Domain;
using Alcedo.Core.Domain.DomException;

namespace Alcedo.Framework.Shared.Domain.Entity;

public abstract class MasterDataAggregateRootBase : ReferenceDataAggregateRootBase
{
    public string Name { get; protected set; }

    public MasterDataStateValue Status { get; protected set; }

    protected virtual StateFlowBase<MasterDataStateValue, MasterDataStateTransitionTrigger> StatusFlow
    {
        get => new MasterDataDefaultStatusFlow();
    }

    protected virtual StateActionGuardBase<MasterDataStateValue, MasterDataStateAction> ActionGuard
    {
        get => new MasterDataDefaultOperationGuard();
    }

    public OperatorRecord Creator { get; protected set; }

    public OperatorRecord Updater { get; protected set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    protected MasterDataAggregateRootBase()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    {
    }

    protected MasterDataAggregateRootBase(
        string code,
        string name,
        OperatorRecord creator) : base(code)
    {
        Name = name;
        Status = MasterDataStates.Transient;
        Creator = creator;
        Updater = creator
            .Clone(); //Although Creator and updater has the same type, have to use different instance, since the initial one is applied to Creator already;
        ExecuteTransition(MasterDataStateTransitionTriggers.Create, creator);
    }

    protected void Update(string name, OperatorRecord updater)
    {
        Name = name;
        Updater = updater;
        ExecuteAction(MasterDataStateActions.Update, updater);
    }

    protected void ExecuteTransition(MasterDataStateTransitionTrigger trigger, OperatorRecord oper,
        string? note = null)
    {
        var from = Status;
        Status = StatusFlow.GetNextState(from, trigger) ??
                 throw new DomErrStatusInvalid(from.Value, trigger.Value);
        RecordAudit(trigger.Value, from, Status, note, oper);
    }

    protected void ExecuteAction(MasterDataStateAction action, OperatorRecord oper, string? note = null)
    {
        if (!ActionGuard.IsAllowed(Status, action))
        {
            throw new DomErrStatusActionInvalid(Status.Value, action.Value);
        }

        RecordAudit(action.Value, Status, Status, note, oper);
    }

    protected void RecordAudit(
        string operation,
        MasterDataStateValue fromStatus,
        MasterDataStateValue toStatus,
        string? note,
        OperatorRecord oper)
    {
        var evt = new MasterDataAuditLogEvent(
            Id,
            AggregateRootMetadataCenter.GetMetaData(this.GetType()).Identity,
            operation,
            fromStatus.Value,
            toStatus.Value,
            note,
            oper);
        AddEvent(evt);
    }

    #region Status Entry

    public virtual void Enable(OperatorRecord oper)
        => ExecuteTransition(MasterDataStateTransitionTriggers.Enable, oper, null);

    public virtual void Disable(OperatorRecord oper)
        => ExecuteTransition(MasterDataStateTransitionTriggers.Disable, oper, null);

    public virtual void Delete(OperatorRecord oper)
        => ExecuteAction(MasterDataStateActions.Delete, oper, null);

    #endregion
}

#region StatusMachine

public sealed record MasterDataStateValue(string Value) : StateValue(Value);

public sealed record MasterDataStateTransitionTrigger(string Value) : StateTransitionTrigger(Value);

public class MasterDataStates : IStateValuesCollection
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