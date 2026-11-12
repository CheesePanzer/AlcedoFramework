namespace Alcedo.Core.Abstractions;

public record ContextUser
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "Anonymous";
    public bool IsAuthenticated { get; set; }
}