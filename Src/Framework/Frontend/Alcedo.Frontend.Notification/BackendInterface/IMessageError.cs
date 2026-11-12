using Alcedo.Frontend.Abstraction;
using Alcedo.Frontend.Abstraction.BackednInterface;
using Alcedo.Frontend.Abstraction.Dto;
using Alcedo.Frontend.Abstraction.SearchQuery;

namespace Alcedo.Frontend.Notification.BackendInterface;

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

public record MessageErrorSearchQuery : EntitySearchQueryBase
{
    public string? MessageTypeCode { get; set; }
}

[ApiClient(Area = "notification", Controller = "messageerror", Version = 1)]
public interface IMessageError : IPagedQueryController<MessageErrorDto, MessageErrorSearchQuery>
{
    [HttpPut("{id}/event:retry")]
    Task RetryErrorMsg([ToRoute] Guid id, CancellationToken cancellationToken = default);

    [HttpDelete("{id}")]
    Task DeleteErrorMsg([ToRoute] Guid id, CancellationToken cancellationToken = default);
}