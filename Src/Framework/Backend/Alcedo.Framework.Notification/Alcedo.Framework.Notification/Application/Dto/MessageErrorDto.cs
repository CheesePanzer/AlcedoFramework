using Alcedo.Core.Application.Dto;

namespace Alcedo.Framework.Notification.Application.Dto;

public record MessageErrorDto : EntityDtoBase
{
    public required string MessageTypeCode { get; init; }
    
    public required string PayloadJson { get; init; }
    
    public required string RecipientName { get; init; }
    
    public required Guid RecipientId { get; init; }
    
    public string? RecipientType { get; init; }
    
    public string? Email { get; init; }
    
    public string? Phone { get; init; }
    
    public required string ErrorMsg { get; init; }
    
    public required DateTimeOffset FailedAt { get; init; }
}