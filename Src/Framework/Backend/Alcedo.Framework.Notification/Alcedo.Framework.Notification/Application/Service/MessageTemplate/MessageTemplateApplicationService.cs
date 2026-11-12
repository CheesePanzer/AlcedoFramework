using Alcedo.Core.Abstractions;
using Alcedo.Core.Application;
using Alcedo.Core.Application.SearchQuery;
using Alcedo.Core.Domain.DomException;
using Alcedo.Framework.Notification.Application.Command;
using Alcedo.Framework.Notification.Application.Dto;
using Alcedo.Framework.Notification.Application.Helper;
using Alcedo.Framework.Notification.Application.ICacheStore;
using Alcedo.Framework.Notification.Application.SearchQuery;
using Alcedo.Framework.Notification.Domain.Entity;
using Alcedo.Framework.Shared;
using Alcedo.Framework.Shared.Application;
using Alcedo.Framework.Shared.Application.Components.MasterData;
using Alcedo.Framework.Shared.Application.Dto;
using Alcedo.Framework.Shared.Application.SearchQuery;
using Alcedo.Framework.Shared.Application.Service;

namespace Alcedo.Framework.Notification.Application.Service.MessageTemplate;

public interface IMessageTemplateApplicationService : IMasterDataApplicationService<MessageTemplateDto,
    MessageTemplateSearchQuery, MessageTemplateCreateCommand,
    MessageTemplateUpdateCommand>
{
    Task<MessageTemplateTestRenderResult> TestMessageTemplateRenderAsync(Guid templateId, string jsonPayload);
}

[RegisterAs(typeof(IMessageTemplateApplicationService))]
internal sealed class MessageTemplateApplicationService : IMessageTemplateApplicationService
{
    private readonly MasterDataSearchServiceComponent<MessageTemplateAggRoot, MessageTemplateDto,
        MessageTemplateSearchQuery> _search;

    private readonly MessageTemplateEditServiceComponent _edit;

    private readonly MasterDataStatusServiceComponent<MessageTemplateAggRoot> _status;

    private readonly MasterDataLogSearchService<MessageTemplateAggRoot> _log;

    private readonly ITemplateMemoryCacheProvider _templateCacheProvider;

    public MessageTemplateApplicationService(
        MasterDataSearchServiceComponent<MessageTemplateAggRoot, MessageTemplateDto, MessageTemplateSearchQuery> search,
        MessageTemplateEditServiceComponent edit, MasterDataStatusServiceComponent<MessageTemplateAggRoot> status,
        MasterDataLogSearchService<MessageTemplateAggRoot> log,
        ITemplateMemoryCacheProvider templateCacheProvider)
    {
        _search = search;
        _edit = edit;
        _status = status;
        _log = log;
        _templateCacheProvider = templateCacheProvider;
    }

    public Task<PagedResult<MessageTemplateDto>> GetPagedDtosAsync(MessageTemplateSearchQuery query,
        SortQuery sortQuery, PaginationQuery pagiQuery,
        CancellationToken ct = default)
        => _search.GetPagedDtosAsync(query, sortQuery, pagiQuery, ct);

    public Task<MessageTemplateDto> GetDtoByIdAsync(Guid id, CancellationToken ct = default)
        => _search.GetDtoByIdAsync(id, ct);

    public Task<Guid> CreateAsync(MessageTemplateCreateCommand command, ContextOper oper,
        CancellationToken ct = default)
        => _edit.CreateAsync(command, oper, ct);

    public Task UpdateByIdAsync(Guid id, MessageTemplateUpdateCommand command, ContextOper oper,
        CancellationToken ct = default)
        => _edit.UpdateByIdAsync(id, command, oper, ct);

    public Task DeleteByIdAsync(Guid id, ContextOper oper, CancellationToken ct = default)
        => _edit.DeleteByIdAsync(id, oper, ct);

    public Task EnableByIdAsync(Guid id, ContextOper oper, CancellationToken ct = default)
        => _status.EnableByIdAsync(id, oper, ct);

    public Task DisableByIdAsync(Guid id, ContextOper oper, CancellationToken ct = default)
        => _status.DisableByIdAsync(id, oper, ct);

    public Task<PagedResult<MasterDataAuditLogDto>> GetPagedAuditLogsAsync(MasterDataLogSearchQuery query,
        SortQuery sortQuery, PaginationQuery pagiQuery,
        CancellationToken ct = default)
        => _log.GetPagedDtosAsync(query, sortQuery, pagiQuery, ct);

    public async Task<MessageTemplateTestRenderResult> TestMessageTemplateRenderAsync(Guid templateId,
        string jsonPayload)
    {
        var template = await _templateCacheProvider.GetOrFallbackDbAsync(templateId)
                       ?? throw new DomErrResourceNotFound<MessageTemplateAggRoot>(templateId);

        var result =
            await MessageTemplateRenderHelper.RenderAsync(template.TitleTemplate, template.BodyTemplate, jsonPayload);

        return new MessageTemplateTestRenderResult()
        {
            Title = result.title,

            Body = result.body,
        };
    }
}