# Alcedo.Core.Domain — State Machine

Alcedo provides a lightweight, type-safe state machine for modeling aggregate status lifecycles. It separates **state transitions** (status changes) from **action guards** (operations permitted in a given status).

---

## Table of Contents

- [Core Types](#core-types)
  - [StateValue](#statevalue)
  - [StateTransitionTrigger](#statetransitiontrigger)
  - [StateTransitionRule](#statetransitionrule)
  - [StateFlowBase](#stateflowbase)
  - [StateAction / StateActionGuardBase](#stateaction--stateactionguardbase)
- [Building a State Machine](#building-a-state-machine)
- [Integration with Aggregates](#integration-with-aggregates)
- [Master Data State Machine](#master-data-state-machine)
- [Design Principles](#design-principles)

---

## Core Types

All state machine primitives derive from `ValueAtom` (see Abstractions documentation), making them immutable, comparable, and JSON-serializable as plain strings.

### StateValue

Represents a discrete status in the lifecycle:

```csharp
public abstract record StateValue(string Value) : ValueAtom(Value);
```

**Concrete example:**

```csharp
public sealed record MasterDataStateValue(string Value) : StateValue(Value);

public class MasterDataStates
{
    public static readonly MasterDataStateValue Draft = new("Draft");
    public static readonly MasterDataStateValue Enabled = new("Enabled");
    public static readonly MasterDataStateValue Disabled = new("Disabled");
}
```

### StateTransitionTrigger

Represents an event or command that causes a status change:

```csharp
public abstract record StateTransitionTrigger(string Value) : ValueAtom(Value);
```

**Example:**

```csharp
public sealed record MasterDataStateTransitionTrigger(string Value) : StateTransitionTrigger(Value);

public class MasterDataStateTransitionTriggers
{
    public static readonly MasterDataStateTransitionTrigger Create = new("Create");
    public static readonly MasterDataStateTransitionTrigger Enable = new("Enable");
    public static readonly MasterDataStateTransitionTrigger Disable = new("Disable");
}
```

### StateTransitionRule

A single directed edge in the state graph:

```csharp
public record StateTransitionRule<TState, TTrigger>(TState From, TTrigger Operation, TState To)
    where TState : StateValue
    where TTrigger : StateTransitionTrigger;
```

**Example:** Draft → Enable → Enabled

```csharp
new StateTransitionRule<MasterDataStateValue, MasterDataStateTransitionTrigger>(
    MasterDataStates.Draft,
    MasterDataStateTransitionTriggers.Enable,
    MasterDataStates.Enabled
)
```

### StateFlowBase

The compiled state graph. Subclasses define `Transitions` as a read-only list of rules. The base class precomputes two lookup tables via lazy initialization:

```csharp
public abstract class StateFlowBase<TState, TTrigger>
    where TState : StateValue
    where TTrigger : StateTransitionTrigger
{
    protected abstract IReadOnlyList<StateTransitionRule<TState, TTrigger>> Transitions { get; }

    public bool CheckOperationValidity(TState status, TTrigger operation);
    public TState? GetNextState(TState status, TTrigger operation);
    public IReadOnlyList<TTrigger> GetAllowedOperations(TState status);
}
```

| Method | Returns |
|--------|---------|
| `CheckOperationValidity` | `true` if a transition exists for `(status, operation)` |
| `GetNextState` | The target state, or `null` if no transition exists |
| `GetAllowedOperations` | All triggers valid from the given state |

### StateAction / StateActionGuardBase

Not all operations change status. Some actions (update, delete, archive) are permitted or denied based on the **current** status.

```csharp
public abstract record StateAction(string Value) : ValueAtom(Value);

public abstract class StateActionGuardBase<TState, TAction>
{
    protected virtual IReadOnlyDictionary<string, IReadOnlySet<TState>> Rules { get; }

    public bool IsAllowed(TState currentState, TAction action);
}
```

**Rules format:** `action → set of permitted states`

```csharp
[MasterDataStateActions.Update.Value] = new HashSet<MasterDataStateValue>
    { MasterDataStates.Draft, MasterDataStates.Enabled, MasterDataStates.Disabled }
```

This means "Update is allowed when status is Draft, Enabled, or Disabled."

---

## Building a State Machine

### 1. Define Values, Triggers, and Actions

```csharp
public sealed record OrderState(string Value) : StateValue(Value);
public sealed record OrderTrigger(string Value) : StateTransitionTrigger(Value);
public sealed record OrderAction(string Value) : StateAction(Value);

public class OrderStates
{
    public static readonly OrderState Pending = new("Pending");
    public static readonly OrderState Confirmed = new("Confirmed");
    public static readonly OrderState Shipped = new("Shipped");
    public static readonly OrderState Cancelled = new("Cancelled");
}

public class OrderTriggers
{
    public static readonly OrderTrigger Confirm = new("Confirm");
    public static readonly OrderTrigger Ship = new("Ship");
    public static readonly OrderTrigger Cancel = new("Cancel");
}

public class OrderActions
{
    public static readonly OrderAction Edit = new("Edit");
    public static readonly OrderAction Refund = new("Refund");
}
```

### 2. Define the Flow

```csharp
public class OrderStateFlow : StateFlowBase<OrderState, OrderTrigger>
{
    protected override IReadOnlyList<StateTransitionRule<OrderState, OrderTrigger>> Transitions =
    [
        new(OrderStates.Pending, OrderTriggers.Confirm, OrderStates.Confirmed),
        new(OrderStates.Confirmed, OrderTriggers.Ship, OrderStates.Shipped),
        new(OrderStates.Pending, OrderTriggers.Cancel, OrderStates.Cancelled),
        new(OrderStates.Confirmed, OrderTriggers.Cancel, OrderStates.Cancelled),
    ];
}
```

### 3. Define the Action Guard

```csharp
public class OrderActionGuard : StateActionGuardBase<OrderState, OrderAction>
{
    protected override IReadOnlyDictionary<string, IReadOnlySet<OrderState>> Rules =
        new Dictionary<string, IReadOnlySet<OrderState>>
        {
            [OrderActions.Edit.Value] = new HashSet<OrderState>
                { OrderStates.Pending, OrderStates.Confirmed },
            [OrderActions.Refund.Value] = new HashSet<OrderState>
                { OrderStates.Shipped, OrderStates.Cancelled },
        };
}
```

### 4. Integrate into Aggregate

```csharp
internal sealed class OrderAgg : AggregateRoot
{
    public OrderState Status { get; private set; } = OrderStates.Pending;

    private static readonly OrderStateFlow _flow = new();
    private static readonly OrderActionGuard _guard = new();

    public void Confirm()
    {
        Status = _flow.GetNextState(Status, OrderTriggers.Confirm)
            ?? throw new DomErrStatusInvalid(Status.Value, OrderTriggers.Confirm.Value);
    }

    public void EditLineItems()
    {
        if (!_guard.IsAllowed(Status, OrderActions.Edit))
            throw new DomErrStatusActionInvalid(Status.Value, OrderActions.Edit.Value);

        // Perform edit...
    }
}
```

---

## Integration with Aggregates

### The Master Data Pattern

Alcedo provides `MasterDataAggregateRootBase` (in `Alcedo.Framework.Shared`) as a reusable base for entities with standard Draft → Enabled → Disabled lifecycles. It demonstrates how to integrate the state machine into an aggregate:

```csharp
public abstract class MasterDataAggregateRootBase : ReferenceDataAggregateRootBase
{
    public MasterDataStateValue Status { get; protected set; }

    protected virtual StateFlowBase<MasterDataStateValue, MasterDataStateTransitionTrigger> StatusFlow
        => new MasterDataDefaultStatusFlow();

    protected virtual StateActionGuardBase<MasterDataStateValue, MasterDataStateAction> ActionGuard
        => new MasterDataDefaultOperationGuard();

    protected void ExecuteTransition(MasterDataStateTransitionTrigger trigger, Guid operId, string operSnap,
        string? note = null)
    {
        var from = Status;
        Status = StatusFlow.GetNextState(from, trigger)
            ?? throw new DomErrStatusInvalid(from.Value, trigger.Value);
        RecordAudit(trigger.Value, from, Status, note, operId, operSnap);
    }

    protected void ExecuteAction(MasterDataStateAction action, Guid operId, string operSnap, string? note = null)
    {
        if (!ActionGuard.IsAllowed(Status, action))
            throw new DomErrStatusActionInvalid(Status.Value, action.Value);

        RecordAudit(action.Value, Status, Status, note, operId, operSnap);
    }
}
```

**Key design points:**

- `ExecuteTransition` changes `Status` and emits an audit event
- `ExecuteAction` validates the action is permitted in the current status, but does **not** change status
- Both methods record audit log events automatically
- Subclasses can override `StatusFlow` and `ActionGuard` to customize behavior

### Extending the Default Flow

```csharp
internal sealed class UserAgg : MasterDataAggregateRootBase
{
    // Use a stricter guard (e.g. disallow Delete when Disabled)
    protected override StateActionGuardBase<MasterDataStateValue, MasterDataStateAction> ActionGuard
        => new UserActionGuard();

    // Add custom status entry methods
    public void Lock(Guid operId, string operSnap)
    {
        IsLocked = true;
        ExecuteAction(UserStateActions.Lock, operId, operSnap);
    }
}
```

---

## Master Data State Machine

The framework provides a default master data lifecycle out of the box:

### Default Status Flow

```
Transient --Create--> Draft --Enable--> Enabled --Disable--> Disabled --Enable--> Enabled
```

### Default Action Guard

| Action | Permitted In |
|--------|-------------|
| Update | Draft, Enabled, Disabled |
| Delete | Draft, Disabled |

### Alternative Guards

| Guard | Difference |
|-------|-----------|
| `MasterDataDefaultOperationGuard` | Delete allowed in Draft and Disabled |
| `MasterDataArchiveOperationGuard` | Delete only in Draft; adds Archive in Disabled |

Subclasses add actions via `Additional` dictionary:

```csharp
internal class UserActionGuard : MasterDataArchiveOperationGuard
{
    protected override IReadOnlyDictionary<string, IReadOnlySet<MasterDataStateValue>> Additional =
        new Dictionary<string, IReadOnlySet<MasterDataStateValue>>
        {
            [UserStateActions.Lock.Value] = new HashSet<MasterDataStateValue>
                { MasterDataStates.Enabled, MasterDataStates.Disabled },
            [UserStateActions.ResetPassword.Value] = new HashSet<MasterDataStateValue>
                { MasterDataStates.Enabled, MasterDataStates.Disabled },
        };
}
```

---

## Design Principles

1. **Status and action are separate concerns.** A state transition changes status; an action guard validates behavior without changing status. This avoids awkward pseudo-transitions like "Enabled → Edit → Enabled."

2. **Type-safe by design.** `StateFlowBase<TState, TTrigger>` prevents mixing states and triggers from different domains at compile time.

3. **Immutable rules.** Transition lists and guard dictionaries are defined once (often as static readonly) and never mutated. The lazy cache inside `StateFlowBase` ensures lookup performance without startup cost.

4. **Extensible via inheritance.** `MasterDataDefaultStatusFlow` splits rules into `Base` and `Additional`, allowing subclasses to extend without copying the entire rule set.
