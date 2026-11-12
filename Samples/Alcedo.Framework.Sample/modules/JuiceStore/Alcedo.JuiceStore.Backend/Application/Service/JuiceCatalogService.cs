using Alcedo.Core.Abstractions;
using Alcedo.Core.Application;
using Alcedo.Core.Application.SearchQuery;
using Alcedo.JuiceStore.Backend.Application.Dto;
using Alcedo.JuiceStore.Backend.Application.SearchQuery;

namespace Alcedo.JuiceStore.Backend.Application.Service;

public interface IJuiceCatalogService
{
    Task<PagedResult<JuiceCatalogItemDto>> GetPageAsync(JuiceCatalogQuery query, PaginationQuery pagination,
        CancellationToken ct = default);

    Task<JuiceCatalogItemDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
}

[RegisterAs(typeof(IJuiceCatalogService))]
internal sealed class JuiceCatalogService(IJuiceCatalogQueryRepository repository) : IJuiceCatalogService
{
    public Task<PagedResult<JuiceCatalogItemDto>> GetPageAsync(JuiceCatalogQuery query,
        PaginationQuery pagination, CancellationToken ct = default) =>
        repository.GetPageAsync(query, pagination, ct);

    public Task<JuiceCatalogItemDto?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        repository.GetByIdAsync(id, ct);
}

public interface IJuiceCatalogQueryRepository
{
    Task<PagedResult<JuiceCatalogItemDto>> GetPageAsync(JuiceCatalogQuery query, PaginationQuery pagination,
        CancellationToken ct = default);

    Task<JuiceCatalogItemDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
}
