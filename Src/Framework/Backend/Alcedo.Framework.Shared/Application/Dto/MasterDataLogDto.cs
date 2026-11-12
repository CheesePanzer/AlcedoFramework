using Alcedo.Core.Application.Dto;

namespace Alcedo.Framework.Shared.Application.Dto;

public sealed record MasterDataAuditLogDto : EntityDtoBase
{
    public required Guid EntityId { get; set; }

    public required string OperationName { get; set; }

    public required string FromStatusName { get; set; }

    public required string ToStatusName { get; set; }

    public required DateTimeOffset OperatedAt { get; set; }

    public required OperatorDto Operator { get; set; }

    public required string? Note { get; set; }
}