using System.Text.Json;
using Alcedo.Core.Abstractions;
using Alcedo.Core.Application;
using Alcedo.Core.Application.AppException;
using Alcedo.Core.Application.SearchQuery;
using Alcedo.Core.Application.Tools;
using Alcedo.Core.Domain.DomException;
using Alcedo.Framework.Identity.Contracts;
using Alcedo.Framework.Notification.Application.Command;
using Alcedo.Framework.Notification.Application.Dto;
using Alcedo.Framework.Notification.Application.IQueryRepository;
using Alcedo.Framework.Notification.Application.SearchQuery;
using Alcedo.Framework.Notification.Contracts;
using Alcedo.Framework.Notification.Domain.Entity;
using Alcedo.Framework.Notification.Domain.Service;
using Alcedo.Framework.Shared;
using Alcedo.Framework.Shared.Application;
using Alcedo.Framework.Shared.Application.Components.MasterData;
using Alcedo.Framework.Shared.Application.Dto;
using Alcedo.Framework.Shared.Application.SearchQuery;
using Alcedo.Framework.Shared.Application.Service;
using Alcedo.Framework.Shared.Domain.IRepositoryBase;

namespace Alcedo.Framework.Notification.Application.Service.MessageConfig;

public interface IMessageConfigApplicationService : IMasterDataApplicationService<MessageConfigDto,
    MessageConfigSearchQuery, MessageConfigCreateCommand,
    MessageConfigUpdateCommand>
{
    Task BatchAssignMessageTemplatesAsync(Guid id, IReadOnlyCollection<Guid> messageTemplateIds,
        ContextOper oper,
        CancellationToken ct = default);

    Task BatchRemoveMessageTemplatesAsync(Guid id, IReadOnlyCollection<Guid> messageTemplateIds,
        ContextOper oper,
        CancellationToken ct = default);

    Task ClearMessageTemplatesAsync(Guid id,
        ContextOper oper,
        CancellationToken ct = default);

    Task<IReadOnlyList<MessageTemplateDto>> GetMessageTemplateDtosByMessageConfigIdAsync(Guid configId,
        CancellationToken ct = default);

    Task TestMessageConfigAsync(Guid configId, string jsonPayload, ContextOper oper, CancellationToken ct = default);

    Task SetSkipByIdAsync(Guid id, ContextOper oper, CancellationToken ct = default);

    Task SetUnSkipByIdAsync(Guid id, ContextOper oper, CancellationToken ct = default);
}

[RegisterAs(typeof(IMessageConfigApplicationService))]
internal sealed class MessageConfigApplicationService : IMessageConfigApplicationService
{
    private readonly MasterDataSearchServiceComponent<MessageConfigAggRoot, MessageConfigDto,
        MessageConfigSearchQuery> _search;

    private readonly MessageConfigEditServiceComponent _edit;

    private readonly MasterDataStatusServiceComponent<MessageConfigAggRoot> _status;

    private readonly MasterDataLogSearchService<MessageConfigAggRoot> _log;

    private readonly MessageConfigDomainService _domain;

    private readonly IMasterDataRepositoryBase<MessageConfigAggRoot> _repository;

    private readonly IMessageConfigQueryRepository _configQueryRepository;

    private readonly IUserQueryIdentityService _identityService;

    private readonly IUnitOfWorkExecutor _executor;

    public MessageConfigApplicationService(
        MasterDataSearchServiceComponent<MessageConfigAggRoot, MessageConfigDto, MessageConfigSearchQuery> search,
        MessageConfigEditServiceComponent edit, MasterDataStatusServiceComponent<MessageConfigAggRoot> status,
        MasterDataLogSearchService<MessageConfigAggRoot> log, MessageConfigDomainService domain,
        IMessageConfigQueryRepository configQueryRepository, IUnitOfWorkExecutor executor,
        IMasterDataRepositoryBase<MessageConfigAggRoot> repository, IUserQueryIdentityService identityService)
    {
        _search = search;
        _edit = edit;
        _status = status;
        _log = log;
        _domain = domain;
        _configQueryRepository = configQueryRepository;
        _executor = executor;
        _repository = repository;
        _identityService = identityService;
    }

    public Task<PagedResult<MessageConfigDto>> GetPagedDtosAsync(MessageConfigSearchQuery query,
        SortQuery sortQuery, PaginationQuery pagiQuery,
        CancellationToken ct = default)
        => _search.GetPagedDtosAsync(query, sortQuery, pagiQuery, ct);

    public Task<MessageConfigDto> GetDtoByIdAsync(Guid id, CancellationToken ct = default)
        => _search.GetDtoByIdAsync(id, ct);

    public Task<Guid> CreateAsync(MessageConfigCreateCommand command, ContextOper oper, CancellationToken ct = default)
        => _edit.CreateAsync(command, oper, ct);

    public Task UpdateByIdAsync(Guid id, MessageConfigUpdateCommand command, ContextOper oper,
        CancellationToken ct = default)
        => _edit.UpdateByIdAsync(id, command, oper, ct);

    public Task DeleteByIdAsync(Guid id, ContextOper oper, CancellationToken ct = default)
        => _edit.DeleteByIdAsync(id, oper, ct);

    public Task EnableByIdAsync(Guid id, ContextOper oper, CancellationToken ct = default)
        => _status.EnableByIdAsync(id, oper, ct);

    public Task DisableByIdAsync(Guid id, ContextOper oper, CancellationToken ct = default)
        => _status.DisableByIdAsync(id, oper, ct);

    public Task SetSkipByIdAsync(Guid id, ContextOper oper, CancellationToken ct = default)
        => _executor.ExecuteAsync(async () =>
        {
            var config = await _repository.GetByIdForUpdateAsync(id, ct) ??
                         throw new DomErrResourceNotFound<MessageConfigAggRoot>(id);
            
            config.SetConfigSkipped(oper.ToRecord());
        }, ct);
    
    public Task SetUnSkipByIdAsync(Guid id, ContextOper oper, CancellationToken ct = default)
        => _executor.ExecuteAsync(async () =>
        {
            var config = await _repository.GetByIdForUpdateAsync(id, ct) ??
                         throw new DomErrResourceNotFound<MessageConfigAggRoot>(id);
            
            config.SetConfigNotSkipped(oper.ToRecord());
        }, ct);

    public Task<PagedResult<MasterDataAuditLogDto>> GetPagedAuditLogsAsync(MasterDataLogSearchQuery query,
        SortQuery sortQuery, PaginationQuery pagiQuery,
        CancellationToken ct = default)
        => _log.GetPagedDtosAsync(query, sortQuery, pagiQuery, ct);

    public Task BatchAssignMessageTemplatesAsync(Guid id, IReadOnlyCollection<Guid> messageTemplateIds,
        ContextOper oper,
        CancellationToken ct = default)
        => _domain.BatchAssignMessageTemplatesAsync(id, messageTemplateIds, oper, ct);

    public Task BatchRemoveMessageTemplatesAsync(Guid id, IReadOnlyCollection<Guid> messageTemplateIds,
        ContextOper oper,
        CancellationToken ct = default)
        => _domain.BatchRemoveMessageTemplatesAsync(id, messageTemplateIds, oper, ct);

    public Task ClearMessageTemplatesAsync(Guid id,
        ContextOper oper,
        CancellationToken ct = default)
        => _domain.ClearMessageTemplatesAsync(id, oper, ct);

    public Task<IReadOnlyList<MessageTemplateDto>> GetMessageTemplateDtosByMessageConfigIdAsync(Guid configId,
        CancellationToken ct = default)
        => _configQueryRepository.GetMessageTemplateDtosByMessageConfigIdAsync(configId, ct);

    public async Task TestMessageConfigAsync(Guid configId, string jsonPayload, ContextOper oper, CancellationToken ct = default)
    {
        var userInfo = await _identityService.GetUserInfoAsync(oper.Id, ct);

        var msgConfig = await _repository.GetByIdNoTrackingAsync(configId, ct) ??
                        throw new DomErrResourceNotFound<MessageConfigAggRoot>(configId);

        if (msgConfig.TemplateIds.Count == 0)
        {
            throw new AppErrGeneric("Need At least one template");
        }

        var recipient = new RecipientContext(userInfo.Name, userInfo.Id, email: userInfo.Email);

        if (!recipient.CanEmail && !recipient.CanInApp && !recipient.CanPhone)
        {
            throw new AppErrGeneric("Operator doesn't have any of the addresses: Email/Phone number");
        }

        Type payloadType = MessageTypeMetadataCenter.GetMetadata(msgConfig.MessageTypeCode).PayloadType;

        object? payloadInstance = null;
        
        var serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = null
        };
        
        if (!string.IsNullOrWhiteSpace(jsonPayload))
        {
            try
            {
                payloadInstance = JsonSerializer.Deserialize(jsonPayload, payloadType, serializerOptions);
            }
            catch (JsonException ex)
            {
                throw new AppErrGeneric($"Failed to deserialize provided payload JSON into type {payloadType.Name}. Error: {ex.Message}");
            }
        }
        
        if (payloadInstance == null)
        {
            payloadInstance = Activator.CreateInstance(payloadType);
        }
        
        string testJsonString = JsonSerializer.Serialize(payloadInstance, payloadType, serializerOptions);
        
        var notificationEvt = new NotificationDispatchEvent(msgConfig.MessageTypeCode, testJsonString, recipient);

        await _executor.ExecuteAsync(() => { _executor.AddApplicationEvent(notificationEvt); }, ct);
    }
}