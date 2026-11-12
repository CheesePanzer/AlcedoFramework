namespace Alcedo.Core.Abstractions;

public abstract record EventRoot
{
    public Guid EventId { get; init; } = Guid.NewGuid();

    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}

public static class EventBaseExtensions
{
    public static string GetAssemblyType(this EventRoot eventRoot)
    {
        return eventRoot.GetType().AssemblyQualifiedName!;
    }

    public static string GetShortName(this EventRoot eventRoot)
    {
        return eventRoot.GetType().Name!;
    }
}