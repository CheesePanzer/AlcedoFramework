using Alcedo.Framework.Shared.Domain.Entity;

namespace Alcedo.Framework.Shared.Application.Dto;

public abstract record MasterDataDtoBase : ReferenceDataDtoBase
{
    public required string Name { get; init; }

    public required MasterDataStateValue Status { get; init; }

    public required OperatorDto Creator { get; init; }

    public required OperatorDto Updater { get; init; }
}