namespace Alcedo.Core.Abstractions;

/// <summary>
/// Base class for all Alcedo framework exceptions.
/// </summary>
public abstract class AlcedoException : Exception
{
    protected virtual string? CustomCode { get; set; }

    /// <summary>
    /// Business-level error code for client consumption.
    /// </summary>
    public string Code => CustomCode ??= GetType().Name;

    public string FriendlyMessage { get; protected init; }

    public string? ClientAction { get; protected init; }

    protected AlcedoException(string message) : base(message)
    {
        FriendlyMessage = message;
    }

    protected AlcedoException(string message, string friendly) : base(message)
    {
        FriendlyMessage = friendly;
    }
}

#region Mapped Exceptions

public abstract class AlcedoBadRequestException : AlcedoException
{
    protected AlcedoBadRequestException(string message) : base(message)
    {
    }

    protected AlcedoBadRequestException(string message, string friendly) : base(message, friendly)
    {
    }
}

public abstract class AlcedoUnauthorizedException : AlcedoException
{
    protected AlcedoUnauthorizedException(string message) : base(message)
    {
    }

    protected AlcedoUnauthorizedException(string message, string friendly) : base(message, friendly)
    {
    }
}

public abstract class AlcedoForbiddenException : AlcedoException
{
    protected AlcedoForbiddenException(string message) : base(message)
    {
    }

    protected AlcedoForbiddenException(string message, string friendly) : base(message, friendly)
    {
    }
}

public abstract class AlcedoNotFoundException : AlcedoException
{
    protected AlcedoNotFoundException(string message) : base(message)
    {
    }

    protected AlcedoNotFoundException(string message, string friendly) : base(message, friendly)
    {
    }
}

public abstract class AlcedoConflictException : AlcedoException
{
    protected AlcedoConflictException(string message) : base(message)
    {
    }

    protected AlcedoConflictException(string message, string friendly) : base(message, friendly)
    {
    }
}

public abstract class AlcedoTooManyRequestsException : AlcedoException
{
    protected AlcedoTooManyRequestsException(string message) : base(message)
    {
    }

    protected AlcedoTooManyRequestsException(string message, string friendly) : base(message, friendly)
    {
    }
}

public abstract class AlcedoInternalException : AlcedoException
{
    protected AlcedoInternalException(string message) : base(message)
    {
    }

    protected AlcedoInternalException(string message, string friendly) : base(message, friendly)
    {
    }
}

#endregion