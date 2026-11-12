namespace Alcedo.Frontend.Abstraction.Dto;

public record OperatorDto
{
    public Guid Id { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string SnapShot => $"[{Code}] {Name}";
}