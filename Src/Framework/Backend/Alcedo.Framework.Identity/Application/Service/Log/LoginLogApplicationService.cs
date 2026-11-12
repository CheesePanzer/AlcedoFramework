using Alcedo.Core.Abstractions;
using Alcedo.Core.Application;
using Alcedo.Core.Application.Component;
using Alcedo.Core.Application.SearchQuery;
using Alcedo.Core.Application.Tools;
using Alcedo.Core.Domain.IRepositoryBase;
using Alcedo.Framework.Identity.Application.Dto.Log;
using Alcedo.Framework.Identity.Application.Event;
using Alcedo.Framework.Identity.Application.SearchQuery;
using Alcedo.Framework.Identity.Domain.Entity;

namespace Alcedo.Framework.Identity.Application.Service.Log;

public interface ILoginLogApplicationService
{
    Task<PagedResult<LoginLogDto>> GetPagedDtosAsync(LoginLogSearchQuery query, SortQuery sortQuery,
        PaginationQuery pagiQuery,
        CancellationToken ct = default);

    Task SaveLoginFromEvtLogAsync(LoginLogEvent @event, CancellationToken ct = default);
}

[RegisterAs(typeof(ILoginLogApplicationService))]
internal class LoginLogApplicationService : ILoginLogApplicationService
{
    private readonly EntitySearchServiceComponent<LoginLogEntity, LoginLogDto, LoginLogSearchQuery>
        _loginLogSearchServiceComponent;

    private readonly IEntityStoreBase<LoginLogEntity> _loginLogStore;

    private readonly IUnitOfWorkExecutor _executor;

    public LoginLogApplicationService(
        EntitySearchServiceComponent<LoginLogEntity, LoginLogDto, LoginLogSearchQuery> loginLogSearchServiceComponent,
        IEntityStoreBase<LoginLogEntity> loginLogStore, IUnitOfWorkExecutor executor)
    {
        _loginLogSearchServiceComponent = loginLogSearchServiceComponent;
        _loginLogStore = loginLogStore;
        _executor = executor;
    }

    public Task<PagedResult<LoginLogDto>> GetPagedDtosAsync(LoginLogSearchQuery query, SortQuery sortQuery,
        PaginationQuery pagiQuery,
        CancellationToken ct = default)
        => _loginLogSearchServiceComponent.GetPagedDtosAsync(query, sortQuery, pagiQuery, ct);

    public async Task SaveLoginFromEvtLogAsync(LoginLogEvent evt, CancellationToken ct = default)
    {
        await _executor.ExecuteAsync(() =>
        {
            var loginLogEntity = new LoginLogEntity(evt.UserId, evt.LoginName, evt.IpAddress, evt.IsSuccess,
                evt.SessionId,
                evt.FailReason, evt.LoginAt);
            _loginLogStore.Add(loginLogEntity);
        }, ct);
    }
}