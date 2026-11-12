using System.ComponentModel.DataAnnotations;
using Alcedo.Core.Presentation.Validation;
using Alcedo.Framework.Shared.Application.Command;

namespace Alcedo.JuiceStore.Backend.Application.Command;

public sealed record JuiceCreateCommand : MasterDataCreateCommandBase
{
    [TextValue]
    public required string Description { get; init; }

    public required string Flavor { get; init; }

    [Range(1, int.MaxValue)]
    public required int VolumeMl { get; init; }

    [Range(typeof(decimal), "0", "9999999999999999")]
    public required decimal Price { get; init; }

    [Url]
    public string? ImageUrl { get; init; }

    public bool IsFeatured { get; init; }

    public int SortOrder { get; init; }
}

public sealed record JuiceUpdateCommand : MasterDataUpdateCommandBase
{
    [TextValue]
    public required string Description { get; init; }

    public required string Flavor { get; init; }

    [Range(1, int.MaxValue)]
    public required int VolumeMl { get; init; }

    [Range(typeof(decimal), "0", "9999999999999999")]
    public required decimal Price { get; init; }

    [Url]
    public string? ImageUrl { get; init; }

    public bool IsFeatured { get; init; }

    public int SortOrder { get; init; }
}
