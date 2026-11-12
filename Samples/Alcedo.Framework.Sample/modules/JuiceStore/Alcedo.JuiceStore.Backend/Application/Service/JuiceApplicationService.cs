using Alcedo.Core.Abstractions;
using Alcedo.Core.Application;
using Alcedo.Core.Application.SearchQuery;
using Alcedo.Framework.Shared;
using Alcedo.Framework.Shared.Application.Components.MasterData;
using Alcedo.Framework.Shared.Application.Dto;
using Alcedo.Framework.Shared.Application.SearchQuery;
using Alcedo.Framework.Shared.Application.Service;
using Alcedo.JuiceStore.Backend.Application.Command;
using Alcedo.JuiceStore.Backend.Application.Dto;
using Alcedo.JuiceStore.Backend.Application.SearchQuery;
using Alcedo.JuiceStore.Backend.Domain;

namespace Alcedo.JuiceStore.Backend.Application.Service;

public interface IJuiceApplicationService : IMasterDataApplicationService<JuiceDto, JuiceSearchQuery,
    JuiceCreateCommand, JuiceUpdateCommand>;

[RegisterAs(typeof(IJuiceApplicationService))]
internal sealed class JuiceApplicationService : IJuiceApplicationService
{
    private readonly MasterDataSearchServiceComponent<JuiceAggRoot, JuiceDto, JuiceSearchQuery> _search;
    private readonly JuiceEditServiceComponent _edit;
    private readonly MasterDataStatusServiceComponent<JuiceAggRoot> _status;
    private readonly MasterDataLogSearchService<JuiceAggRoot> _log;

    public JuiceApplicationService(
        MasterDataSearchServiceComponent<JuiceAggRoot, JuiceDto, JuiceSearchQuery> search,
        JuiceEditServiceComponent edit,
        MasterDataStatusServiceComponent<JuiceAggRoot> status,
        MasterDataLogSearchService<JuiceAggRoot> log)
    {
        _search = search;
        _edit = edit;
        _status = status;
        _log = log;
    }

    public Task<PagedResult<JuiceDto>> GetPagedDtosAsync(JuiceSearchQuery query, SortQuery sortQuery,
        PaginationQuery pagiQuery, CancellationToken ct = default) =>
        _search.GetPagedDtosAsync(query, sortQuery, pagiQuery, ct);

    public Task<JuiceDto> GetDtoByIdAsync(Guid id, CancellationToken ct = default) =>
        _search.GetDtoByIdAsync(id, ct);

    public Task<Guid> CreateAsync(JuiceCreateCommand command, ContextOper oper, CancellationToken ct = default) =>
        _edit.CreateAsync(command, oper, ct);

    public Task UpdateByIdAsync(Guid id, JuiceUpdateCommand command, ContextOper oper,
        CancellationToken ct = default) =>
        _edit.UpdateByIdAsync(id, command, oper, ct);

    public Task DeleteByIdAsync(Guid id, ContextOper oper, CancellationToken ct = default) =>
        _edit.DeleteByIdAsync(id, oper, ct);

    public Task EnableByIdAsync(Guid id, ContextOper oper, CancellationToken ct = default) =>
        _status.EnableByIdAsync(id, oper, ct);

    public Task DisableByIdAsync(Guid id, ContextOper oper, CancellationToken ct = default) =>
        _status.DisableByIdAsync(id, oper, ct);

    public Task<PagedResult<MasterDataAuditLogDto>> GetPagedAuditLogsAsync(MasterDataLogSearchQuery query,
        SortQuery sortQuery, PaginationQuery pagiQuery, CancellationToken ct = default) =>
        _log.GetPagedDtosAsync(query, sortQuery, pagiQuery, ct);
}
