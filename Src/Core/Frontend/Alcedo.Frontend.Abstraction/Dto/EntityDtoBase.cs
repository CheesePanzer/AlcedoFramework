using Alcedo.Frontend.Abstraction.Attributes;

namespace Alcedo.Frontend.Abstraction.Dto;

public abstract record EntityDtoBase
{
    [AutoFormIgnoreRender]
    [AutoTableIgnoreRender]
    public Guid Id { get; set; }
}