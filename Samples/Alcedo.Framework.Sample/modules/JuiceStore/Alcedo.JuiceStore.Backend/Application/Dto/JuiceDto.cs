using Alcedo.Framework.Shared.Application.Dto;

namespace Alcedo.JuiceStore.Backend.Application.Dto;

public sealed record JuiceDto : MasterDataDtoBase
{
    public required string Description { get; init; }

    public required string Flavor { get; init; }

    public required int VolumeMl { get; init; }

    public required decimal Price { get; init; }

    public string? ImageUrl { get; init; }

    public required bool IsFeatured { get; init; }

    public required int SortOrder { get; init; }
}

public sealed record JuiceCatalogItemDto
{
    public required Guid Id { get; init; }

    public required string Code { get; init; }

    public required string Name { get; init; }

    public required string Description { get; init; }

    public required string Flavor { get; init; }

    public required int VolumeMl { get; init; }

    public required decimal Price { get; init; }

    public string? ImageUrl { get; init; }

    public required bool IsFeatured { get; init; }
}
