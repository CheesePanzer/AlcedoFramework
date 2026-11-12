namespace Alcedo.Frontend.Identity.Clients.Perm;

public record PermSearchQuery
{
    public string? Area { get; set; }
    public string? Resource { get; set; }
    public string? Operation { get; set; }

    public string? ResourceDescription { get; set; }
    public string? OperationDescription { get; set; }
}

public sealed record PermDto
{
    public string? Area { get; set; }

    public string Resource { get; set; } = null!;

    public string Operation { get; set; } = null!;

    public string EndpointKey { get; set; } = null!;

    public string ResourceDescription { get; set; } = null!;

    public string OperationDescription { get; set; } = null!;
}