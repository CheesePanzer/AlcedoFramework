using Alcedo.Core.Abstractions;
using Alcedo.Core.Application;
using Alcedo.Core.Application.AppException;
using Alcedo.Core.Application.Component;
using Alcedo.Core.Application.SearchQuery;
using Alcedo.Core.Application.Tools;
using Alcedo.Core.Domain.DomException;
using Alcedo.Core.Domain.IRepositoryBase;
using Alcedo.Framework.Notification.Application.Dto;
using Alcedo.Framework.Notification.Application.SearchQuery;
using Alcedo.Framework.Notification.Contracts;
using Alcedo.Framework.Notification.Domain.Entity;

namespace Alcedo.Framework.Notification.Application.Service.MessageError;

public interface IMessageErrorApplicationService
{
    Task<PagedResult<MessageErrorDto>> GetPagedDtosAsync(MessageErrorSearchQuery query,
        SortQuery sortQuery, PaginationQuery pagiQuery,
        CancellationToken ct = default);
    
    Task<MessageErrorDto> GetDtoByIdAsync(Guid id, CancellationToken ct = default);
    
    Task ConvertToEventAsync(Guid id, CancellationToken ct = default);
    
    Task IgnoreErrorAsync(Guid id, CancellationToken ct = default);
}

[RegisterAs(typeof(IMessageErrorApplicationService))]
internal class MessageErrorApplicationService : IMessageErrorApplicationService
{
    private readonly EntitySearchServiceComponent<MessageErrorEntity, MessageErrorDto, MessageErrorSearchQuery> _search;
    
    private readonly IEntityStoreBase<MessageErrorEntity> _store;
    
    private readonly IUnitOfWorkExecutor _executor;
    
    public MessageErrorApplicationService(EntitySearchServiceComponent<MessageErrorEntity, MessageErrorDto, MessageErrorSearchQuery> search, IUnitOfWorkExecutor executor, IEntityStoreBase<MessageErrorEntity> store)
    {
        _search = search;
        _executor = executor;
        _store = store;
    }

    public Task<PagedResult<MessageErrorDto>> GetPagedDtosAsync(MessageErrorSearchQuery query,
        SortQuery sortQuery, PaginationQuery pagiQuery,
        CancellationToken ct = default)
        => _search.GetPagedDtosAsync(query, sortQuery, pagiQuery, ct);

    public Task<MessageErrorDto> GetDtoByIdAsync(Guid id, CancellationToken ct = default)
        => _search.GetDtoByIdAsync(id, ct);
    
    public Task ConvertToEventAsync(Guid id, CancellationToken ct = default)
        => _executor.ExecuteAsync(async () =>
        {
            var error = await _store.GetByIdAsync(id, ct);

            if (error is null)
            {
                return;
            }

            var recipient = new RecipientContext(
                error.RecipientName,
                error.RecipientId,
                error.RecipientType,
                error.Email,
                error.Phone
                );
            
            var evt = new NotificationDispatchEvent(error.MessageTypeCode, error.PayloadJson, recipient);
            
            _executor.AddApplicationEvent(evt);
            
            _store.Remove(error);
        }, ct);

    public Task IgnoreErrorAsync(Guid id, CancellationToken ct = default)
        => _executor.ExecuteAsync(async () =>
        {
            var error = await _store.GetByIdAsync(id, ct);
            if (error != null)
            {
                _store.Remove(error);  
            }
           
        }, ct);
}