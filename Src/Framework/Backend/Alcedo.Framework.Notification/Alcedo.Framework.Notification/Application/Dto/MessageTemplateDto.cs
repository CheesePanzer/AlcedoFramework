using Alcedo.Framework.Notification.Contracts;
using Alcedo.Framework.Shared.Application.Dto;

namespace Alcedo.Framework.Notification.Application.Dto;

public sealed record MessageTemplateDto : MasterDataDtoBase
{
    public required NotificationChannel Channel { get; init; }

    public required string? TitleTemplate { get; init; }

    public required string? BodyTemplate { get; init; }

    public required string? ExternalTemplateId { get; init; }
}