namespace Alcedo.Frontend.Abstraction.Dto;

public sealed record MasterDataAuditLogDto : EntityDtoBase
{
    public Guid EntityId { get; set; }

    public string OperationName { get; set; } = null!;

    public string FromStatusName { get; set; } = null!;

    public string ToStatusName { get; set; } = null!;

    public DateTimeOffset OperatedAt { get; set; }

    public OperatorDto Operator { get; set; } = null!;

    public string? Note { get; set; }
}