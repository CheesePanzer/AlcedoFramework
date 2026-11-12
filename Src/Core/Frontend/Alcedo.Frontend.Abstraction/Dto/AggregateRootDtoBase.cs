using Alcedo.Frontend.Abstraction.Attributes;

namespace Alcedo.Frontend.Abstraction.Dto;

public abstract record AggregateRootDtoBase : EntityDtoBase
{
    [AutoFormIgnoreRender]
    [AutoTableIgnoreRender]
    public int Version { get; set; }

    [AutoFormIgnoreRender] public DateTimeOffset CreatedAt { get; set; }

    [AutoFormIgnoreRender] public DateTimeOffset UpdatedAt { get; set; }
}