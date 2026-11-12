namespace Alcedo.Framework.Identity.Application.SearchQuery;

public record PermSearchQuery
{
    public string? Area { get; init; }
    public string? Resource { get; init; }
    public string? Operation { get; init; }

    public string? ResourceDescription { get; init; }
    public string? OperationDescription { get; init; }
}