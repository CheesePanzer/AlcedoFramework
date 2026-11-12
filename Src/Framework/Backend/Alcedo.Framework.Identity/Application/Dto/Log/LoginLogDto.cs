using Alcedo.Core.Application.Dto;

namespace Alcedo.Framework.Identity.Application.Dto.Log;

public sealed record LoginLogDto : EntityDtoBase
{
    public required Guid? UserId { get; init; }
    public required string LoginName { get; init; }
    public required string IpAddress { get; init; }
    public required bool IsSuccess { get; init; }
    public required Guid? SessionId { get; init; }
    public required string? FailReason { get; init; }
    public required DateTimeOffset LoginAt { get; init; }
}