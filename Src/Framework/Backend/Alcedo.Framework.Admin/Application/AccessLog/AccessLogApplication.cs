using System.Text.Json;
using Alcedo.Core.Abstractions;
using Alcedo.Core.Application;
using Alcedo.Core.Application.Component;
using Alcedo.Core.Application.SearchQuery;
using Alcedo.Core.Application.Tools;
using Alcedo.Core.Domain.IRepositoryBase;
using Alcedo.Framework.Admin.Domain.Entity;
using Alcedo.Framework.Admin.Infrastructure;
using Alcedo.Framework.Admin.Presentation.Interface;
using Microsoft.Extensions.Options;

namespace Alcedo.Framework.Admin.Application.AccessLog;

public interface IAccessLogApplicationService
{
    Task<PagedResult<AccessLogDto>> GetPagedDtosAsync(AccessLogSearchQuery query, SortQuery sortQuery,
        PaginationQuery pagiQuery,
        CancellationToken ct = default);

    Task FetchAndSaveAccessLogAsync(CancellationToken ct = default);
}

[RegisterAs(typeof(IAccessLogApplicationService))]
internal sealed class AccessLogApplicationService : IAccessLogApplicationService
{
    private readonly IEntityStoreBase<AccessLogEntity> _iAccessLogStore;

    private readonly IUnitOfWorkExecutor _unitOfWorkExecutor;

    private readonly IAccessLogRedisStore _accessLogRedisStore;

    private readonly EntitySearchServiceComponent<AccessLogEntity, AccessLogDto, AccessLogSearchQuery>
        _accessLogSearchServiceComponent;

    private readonly AccessLogOptions _accessLogOptions;

    public AccessLogApplicationService(
        IUnitOfWorkExecutor unitOfWorkExecutor,
        IAccessLogRedisStore accessLogRedisStore,
        IEntityStoreBase<AccessLogEntity> iAccessLogStore,
        EntitySearchServiceComponent<AccessLogEntity, AccessLogDto, AccessLogSearchQuery>
            accessLogSearchServiceComponent,
        IOptions<AccessLogOptions> options)
    {
        _unitOfWorkExecutor = unitOfWorkExecutor;
        _accessLogRedisStore = accessLogRedisStore;
        _iAccessLogStore = iAccessLogStore;
        _accessLogSearchServiceComponent = accessLogSearchServiceComponent;
        _accessLogOptions = options.Value;
    }

    public Task<PagedResult<AccessLogDto>> GetPagedDtosAsync(AccessLogSearchQuery query,
        SortQuery sortQuery, PaginationQuery pagiQuery,
        CancellationToken ct = default) =>
        _accessLogSearchServiceComponent.GetPagedDtosAsync(query, sortQuery, pagiQuery, ct);

    public async Task FetchAndSaveAccessLogAsync(CancellationToken ct = default)
    {
        var rawLogs = await _accessLogRedisStore.DequeueRawLogsAsync(_accessLogOptions.BatchFetchSize);

        if (!rawLogs.Any())
        {
            return;
        }

        try
        {
            var logs = rawLogs
                .Select(json => JsonSerializer.Deserialize<AccessLogEntity>(json))
                .OfType<AccessLogEntity>()
                .ToList();

            await _unitOfWorkExecutor.ExecuteAsync(() => { _iAccessLogStore.AddRange(logs); }, ct);
        }
        catch
        {
            await _accessLogRedisStore.RequeueRawLogsAsync(rawLogs);
            throw;
        }
    }
}