using Alcedo.Core.Abstractions;
using Alcedo.Core.Application;
using Alcedo.Core.Application.SearchQuery;
using Alcedo.Framework.Notification.Application.Dto;
using Alcedo.Framework.Notification.Application.SearchQuery;
using Alcedo.Framework.Notification.Contracts;
using MapsterMapper;

namespace Alcedo.Framework.Notification.Application.Service.MessageType;

public interface
    IMessageTypeApplicationService
{
    PagedResult<MessageTypeDto> GetPagedDtosAsync(MessageTypeSearchQuery query, PaginationQuery pagiQuery);
    
    MessageTypeDto GetDtoByCode(string code);
}

[RegisterAs(typeof(IMessageTypeApplicationService))]
internal sealed class MessageTypeApplicationService : IMessageTypeApplicationService
{
    private readonly IMapper _mapper;

    public MessageTypeApplicationService(IMapper mapper)
    {
        _mapper = mapper;
    }

    public PagedResult<MessageTypeDto> GetPagedDtosAsync(MessageTypeSearchQuery query, PaginationQuery pagiQuery)
    {
        var result =
            MessageTypeMetadataCenter.GetPagedMetadata(pagiQuery.PageNo, pagiQuery.PageSize, query.Code, query.Name);
        var dtos = _mapper.Map<MessageTypeDto[]>(result.Items);
        var pagedResult = new PagedResult<MessageTypeDto>
        {
            PageNo = pagiQuery.PageNo,
            PageSize = pagiQuery.PageSize,
            TotalCount = result.Count,
            Items = dtos
        };

        return pagedResult;
    }

    public MessageTypeDto GetDtoByCode(string code)
    {
        var origin = MessageTypeMetadataCenter.GetMetadata(code);
        
        return _mapper.Map<MessageTypeDto>(origin);
    }
}