using Alcedo.Core.Abstractions;
using Alcedo.Core.Domain.Entity;

namespace Alcedo.Core.Domain.DomException;

public class DomErrGeneric : AlcedoBadRequestException
{
    public DomErrGeneric(string message) : base(message)
    {
    }

    public DomErrGeneric(string message, string friendly) : base(message, friendly)
    {
    }
}

/// <summary>
/// Thrown when an aggregate root cannot be located in the repository.
/// </summary>
public class DomErrResourceNotFound : AlcedoNotFoundException
{
    private const string Friendly = "The requested resource was not found.";

    public DomErrResourceNotFound(string resource, Guid id)
        : base($"{resource} with {id} was not found.", Friendly)
    {
    }

    public DomErrResourceNotFound(string resource, IEnumerable<Guid> ids)
        : base($"{resource}  with ({string.Join(",", ids)}) was not found.", Friendly)
    {
    }
}

/// <summary>
/// Thrown when an aggregate root cannot be located in the repository.
/// </summary>
public class DomErrResourceNotFound<TEntity> : AlcedoNotFoundException
    where TEntity : EntityBase
{
    private const string Friendly = "The requested resource was not found.";

    public DomErrResourceNotFound(string name)
        : base($"{typeof(TEntity).Name} with {name} was not found.", Friendly)
    {
    }

    public DomErrResourceNotFound(Guid id)
        : base($"{typeof(TEntity).Name} with {id} was not found.", Friendly)
    {
    }

    public DomErrResourceNotFound(IEnumerable<string> names)
        : base($"{typeof(TEntity).Name} with ({string.Join(",", names)}) was not found.", Friendly)
    {
    }

    public DomErrResourceNotFound(IEnumerable<Guid> ids)
        : base($"{typeof(TEntity).Name} with ({string.Join(",", ids)}) was not found.", Friendly)
    {
    }
}

/// <summary>
/// Thrown when an aggregate is referenced by other records and cannot be removed.
/// </summary>
public class DomErrAggReferenced : AlcedoConflictException
{
    private const string Friendly = "This resource is currently referenced by others";

    public DomErrAggReferenced()
        : base($"This record is referenced by other records", Friendly)
    {
    }

    public DomErrAggReferenced(string aggregate)
        : base($"{aggregate} is referenced by other records", Friendly)
    {
    }

    public DomErrAggReferenced(string aggregate, string referencer)
        : base($"{aggregate} is referenced by {referencer}", Friendly)
    {
    }
}

/// <summary>
/// Thrown when optimistic concurrency detects a version mismatch.
/// </summary>
public class DomErrAggVersionConflict<TAgg> : AlcedoConflictException
{
    private const string Friendly = "Current resource has been modified, please refresh";

    public DomErrAggVersionConflict(int currentVersion, int expectedVersion)
        : base(
            $"{typeof(TAgg).Name} has been modified, current version: {currentVersion}, expected version:{expectedVersion}.",
            Friendly)
    {
    }
}

/// <summary>
/// Thrown when optimistic concurrency detects a version mismatch.
/// </summary>
public class DomErrAggVersionConflict : AlcedoConflictException
{
    private const string Friendly = "Current resource has been modified, please refresh";

    public DomErrAggVersionConflict(string aggregate, int currentVersion, int expectedVersion)
        : base($"{aggregate} has been modified, current version: {currentVersion}, expected version:{expectedVersion}.",
            Friendly)
    {
    }
}

/// <summary>
/// Thrown when a query returns multiple records where exactly one was expected.
/// </summary>
public class DomErrMultipleFound : AlcedoConflictException
{
    private const string Friendly = "Multiple records found";

    public DomErrMultipleFound(string aggregate)
        : base($"Multiple records found for {aggregate} where one was expected.", Friendly)
    {
    }
}

/// <summary>
/// Thrown when a domain business rule is violated.
/// </summary>
public class DomErrRuleViolation : AlcedoBadRequestException
{
    public DomErrRuleViolation(string rule)
        : base($"Rule violated: {rule}")
    {
    }
}

/// <summary>
/// Thrown when a status transition is not permitted by domain rules.
/// </summary>
public class DomErrStatusInvalid : AlcedoBadRequestException
{
    private const string Friendly = "This status transition is not allowed.";

    public DomErrStatusInvalid(string detail)
        : base($"Invalid status transition: {detail}", Friendly)
    {
    }

    public DomErrStatusInvalid(string status, string action)
        : base($"Invalid status transition: {action} under {status}", Friendly)
    {
    }
}

/// <summary>
/// Thrown when a status transition is not permitted by domain rules.
/// </summary>
public class DomErrStatusActionInvalid : AlcedoBadRequestException
{
    private const string Friendly = "This action is not allowed in the current status.";

    public DomErrStatusActionInvalid(string detail)
        : base($"Invalid action: {detail}", Friendly)
    {
    }

    public DomErrStatusActionInvalid(string status, string action)
        : base($"Invalid action: action {action} is not allowed when {status}", Friendly)
    {
    }
}

/// <summary>
/// Thrown when a unique constraint is violated.
/// </summary>
public class DomErrDuplicateEntry : AlcedoConflictException
{
    public DomErrDuplicateEntry(string field)
        : base($"Duplicate value for: {field}", $"A record with {field} of the same value already exists.")
    {
    }
}

/// <summary>
/// Thrown when a resource is locked by a concurrent operation.
/// </summary>
public class DomErrLockConflict : AlcedoConflictException
{
    private const string Friendly = "The record is locked by another operation.";

    public DomErrLockConflict(string resource, string lockType, TimeSpan timeout)
        : base(
            $"{resource} can't be locked in {lockType} with timeout: {timeout.TotalSeconds} secs, since it is currently locked by another operation.",
            Friendly)
    {
    }
}