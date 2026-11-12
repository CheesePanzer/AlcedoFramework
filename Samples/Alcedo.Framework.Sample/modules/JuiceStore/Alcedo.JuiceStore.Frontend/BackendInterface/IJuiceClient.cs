using Alcedo.Frontend.Abstraction;
using Alcedo.Frontend.Abstraction.Attributes;
using Alcedo.Frontend.Abstraction.BackednInterface;
using Alcedo.Frontend.Abstraction.Command;
using Alcedo.Frontend.Abstraction.Dto;
using Alcedo.Frontend.Abstraction.SearchQuery;

namespace Alcedo.JuiceStore.Frontend.BackendInterface;

[AutoTableLink(nameof(Code), "/juicestore/juices/{Id}")]
public sealed record JuiceDto : MasterDataDtoBase
{
    [AutoTableIgnoreRender]
    public string Description { get; set; } = string.Empty;

    public string Flavor { get; set; } = string.Empty;

    public int VolumeMl { get; set; }

    public decimal Price { get; set; }

    [AutoTableIgnoreRender]
    public string? ImageUrl { get; set; }

    public bool IsFeatured { get; set; }

    public int SortOrder { get; set; }
}

public sealed record JuiceSearchQuery : MasterDataSearchQueryBase
{
    public string? Flavor { get; set; }

    public bool? IsFeatured { get; set; }
}

public sealed record JuiceCreateCommand : MasterDataCreateCommandBase
{
    [AutoFormRenderTextArea(lines: 4, maxLines: 10)]
    [AutoFormCellWidth(12)]
    public string Description { get; set; } = string.Empty;

    public string Flavor { get; set; } = string.Empty;

    [AutoFormNumericStep("1")]
    public int VolumeMl { get; set; } = 250;

    [AutoFormNumericStep("0.01")]
    public decimal Price { get; set; }

    [AutoFormCellWidth(12)]
    public string? ImageUrl { get; set; }

    public bool IsFeatured { get; set; }

    [AutoFormNumericStep("1")]
    public int SortOrder { get; set; }
}

public sealed record JuiceUpdateCommand : MasterDataUpdateCommandBase
{
    [AutoFormRenderTextArea(lines: 4, maxLines: 10)]
    [AutoFormCellWidth(12)]
    public string Description { get; set; } = string.Empty;

    public string Flavor { get; set; } = string.Empty;

    [AutoFormNumericStep("1")]
    public int VolumeMl { get; set; }

    [AutoFormNumericStep("0.01")]
    public decimal Price { get; set; }

    [AutoFormCellWidth(12)]
    public string? ImageUrl { get; set; }

    public bool IsFeatured { get; set; }

    [AutoFormNumericStep("1")]
    public int SortOrder { get; set; }
}

[ApiClient(Area = "juicestore", Controller = "juices", Version = 1)]
public interface IJuiceClient : IMasterDataController<JuiceDto, JuiceSearchQuery, JuiceCreateCommand,
    JuiceUpdateCommand>;
