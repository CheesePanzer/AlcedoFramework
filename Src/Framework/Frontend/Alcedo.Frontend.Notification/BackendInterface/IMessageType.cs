using Alcedo.Frontend.Abstraction;
using Alcedo.Frontend.Abstraction.BackednInterface;

namespace Alcedo.Frontend.Notification.BackendInterface;

public sealed record MessageTypeDto
{
    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string Description { get; set; } = null!;
    
    public string JsonExample { get; set; } = null!;
}

public sealed record MessageTypeSearchQuery
{
    public string? Code { get; set; }

    public string? Name { get; set; }
}

[ApiClient(Area = "notification", Controller = "messagetypes", Version = 1)]
public interface IMessageType : IPagedQueryController<MessageTypeDto, MessageTypeSearchQuery>
{
    [HttpGet("{code}")]
    public Task<MessageTypeDto> GetItemByCode([ToRoute] string code);
}