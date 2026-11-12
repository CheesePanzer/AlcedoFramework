using Alcedo.Core.Abstractions;

namespace Alcedo.Core.Infrastructure.InfraException;

/// <summary>
/// Thrown when an entity type has no corresponding EF Core table mapping registered.
/// Maps to 500.
/// </summary>
public class InfraErrEntityConfigMissing : AlcedoInternalException
{
    public InfraErrEntityConfigMissing(string entityTypeName)
        : base($"No table mapping found for entity type '{entityTypeName}'.")
    {
    }
}

/// <summary>
/// Thrown when a scheduled job is missing its interval configuration.
/// Maps to 500.
/// </summary>
public class InfraErrSchedulerConfigMissing : AlcedoInternalException
{
    public InfraErrSchedulerConfigMissing(string jobName)
        : base($"Scheduler interval is not configured for job '{jobName}'.")
    {
    }
}

/// <summary>
/// Thrown when a scheduled job has an invalid interval configuration.
/// Maps to 500.
/// </summary>
public class InfraErrSchedulerConfigInvalid : AlcedoInternalException
{
    public InfraErrSchedulerConfigInvalid(string jobName)
        : base($"Scheduler interval for job '{jobName}' is invalid.")
    {
    }
}

/// <summary>
/// Thrown when a row lock cannot be acquired immediately.
/// Maps to 409.
/// </summary>
public class InfraErrLockNotAvailable : AlcedoConflictException
{
    public InfraErrLockNotAvailable()
        : base("Resource is currently locked by another operation, please retry.")
    {
    }
}

/// <summary>
/// Thrown when a deadlock is detected between concurrent transactions.
/// Maps to 409.
/// </summary>
public class InfraErrDeadlockDetected : AlcedoConflictException
{
    public InfraErrDeadlockDetected()
        : base("Deadlock detected, please retry.")
    {
    }
}

/// <summary>
/// Thrown when a query is cancelled due to lock wait timeout.
/// Maps to 409.
/// </summary>
public class InfraErrLockTimeout : AlcedoConflictException
{
    public InfraErrLockTimeout()
        : base("Query was cancelled due to lock wait timeout, please retry.")
    {
    }
}

/// <summary>
/// Thrown when a serialization failure occurs under Serializable isolation.
/// Maps to 409.
/// </summary>
public class InfraErrSerializationFailure : AlcedoConflictException
{
    public InfraErrSerializationFailure()
        : base("Serialization failure, please retry.")
    {
    }
}

public class InfraErrColumnNotStorageKey : AlcedoInternalException
{
    public InfraErrColumnNotStorageKey() : base("Column is not a storage column")
    {
        
    }
}