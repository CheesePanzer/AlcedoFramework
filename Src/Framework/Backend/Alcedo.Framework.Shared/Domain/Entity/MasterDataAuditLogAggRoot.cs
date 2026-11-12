using Alcedo.Core.Domain.Entity;
using Alcedo.Core.Domain.Event;

namespace Alcedo.Framework.Shared.Domain.Entity;

public sealed class MasterDataAuditLogEntity : EntityBase
{
    public Guid AggregateId { get; init; }

    public string AggregateIdentity { get; init; }

    public string OperationName { get; init; }

    public string FromStatusName { get; init; }

    public string ToStatusName { get; init; }

    public DateTimeOffset OperatedAt { get; init; }

    public OperatorRecord Operator { get; init; }

    public string? Note { get; init; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    protected MasterDataAuditLogEntity()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    {
    }

    public MasterDataAuditLogEntity(
        Guid aggregateId,
        string aggregateIdentity,
        string operationName,
        string fromStatusName,
        string toStatusName,
        DateTimeOffset operatedAt,
        string? note,
        OperatorRecord oper
    )
    {
        AggregateId = aggregateId;
        AggregateIdentity = aggregateIdentity;
        OperationName = operationName;
        FromStatusName = fromStatusName;
        ToStatusName = toStatusName;
        Note = note;
        OperatedAt = operatedAt;
        Operator = oper;
    }
}

public sealed record MasterDataAuditLogEvent : DomainEventBase
{
    public string AggregateRootIdentity { get; init; }

    public string StatusOperation { get; init; }

    public string FromStatus { get; init; }

    public string ToStatus { get; init; }

    public string? Note { get; init; }

    public OperatorRecord Operator { get; init; }

    public MasterDataAuditLogEvent(
        Guid aggregateId,
        string aggregateRootIdentity,
        string statusOperation,
        string fromStatus,
        string toStatus,
        string? note,
        OperatorRecord @operator) : base(aggregateId)
    {
        AggregateRootIdentity = aggregateRootIdentity;
        StatusOperation = statusOperation;
        FromStatus = fromStatus;
        ToStatus = toStatus;
        Note = note;
        Operator = @operator;
    }
}