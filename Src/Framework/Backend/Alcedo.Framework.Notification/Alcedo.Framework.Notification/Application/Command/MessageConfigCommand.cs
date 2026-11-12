using Alcedo.Framework.Shared.Application.Command;

namespace Alcedo.Framework.Notification.Application.Command;

public sealed record MessageConfigCreateCommand : MasterDataCreateCommandBase
{
    public required string MessageTypeCode { get; init; }
}

public sealed record MessageConfigUpdateCommand : MasterDataUpdateCommandBase
{
    public required string MessageTypeCode { get; init; }
}