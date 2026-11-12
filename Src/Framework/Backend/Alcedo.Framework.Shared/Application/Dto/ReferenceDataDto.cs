using Alcedo.Core.Application.Dto;

namespace Alcedo.Framework.Shared.Application.Dto;

public abstract record ReferenceDataDtoBase : AggregateRootDtoBase
{
    public required string Code { get; init; }
}