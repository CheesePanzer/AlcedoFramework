namespace Alcedo.Frontend.Abstraction.Dto;

public abstract record ReferenceDataDtoBase : AggregateRootDtoBase
{
    public string Code { get; set; } = null!;
}