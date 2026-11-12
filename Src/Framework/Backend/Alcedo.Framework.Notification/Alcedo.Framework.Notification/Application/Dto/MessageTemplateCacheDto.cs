using Alcedo.Core.Application.Dto;
using Alcedo.Framework.Notification.Contracts;

namespace Alcedo.Framework.Notification.Application.Dto;

public sealed record MessageTemplateCacheDto : EntityCacheDtoBase
{
    public required string Code { get; init; }

    public required string Name { get; init; }

    public bool IsActive { get; init; }

    public required NotificationChannel Channel { get; init; }

    public required string? TitleTemplate { get; init; }

    public required string? BodyTemplate { get; init; }

    public required string? ExternalTemplateId { get; init; }
}