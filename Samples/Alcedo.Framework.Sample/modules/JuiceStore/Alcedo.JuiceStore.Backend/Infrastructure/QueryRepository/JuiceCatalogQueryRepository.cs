using Alcedo.Core.Abstractions;
using Alcedo.Core.Application;
using Alcedo.Core.Application.SearchQuery;
using Alcedo.Core.Infrastructure.EfCore;
using Alcedo.Framework.Shared.Domain.Entity;
using Alcedo.JuiceStore.Backend.Application.Dto;
using Alcedo.JuiceStore.Backend.Application.SearchQuery;
using Alcedo.JuiceStore.Backend.Application.Service;
using Alcedo.JuiceStore.Backend.Domain;
using Microsoft.EntityFrameworkCore;

namespace Alcedo.JuiceStore.Backend.Infrastructure.QueryRepository;

[RegisterAs(typeof(IJuiceCatalogQueryRepository))]
internal sealed class JuiceCatalogQueryRepository(AlcedoDbContext context) : IJuiceCatalogQueryRepository
{
    public async Task<PagedResult<JuiceCatalogItemDto>> GetPageAsync(JuiceCatalogQuery query,
        PaginationQuery pagination, CancellationToken ct = default)
    {
        var juices = context.RawEntity<JuiceAggRoot>()
            .AsNoTracking()
            .Where(j => j.Status == MasterDataStates.Enabled);

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var keyword = query.Keyword.Trim();
            juices = juices.Where(j => EF.Functions.ILike(j.Name, $"%{keyword}%") ||
                                       EF.Functions.ILike(j.Description, $"%{keyword}%"));
        }

        if (!string.IsNullOrWhiteSpace(query.Flavor))
        {
            var flavor = query.Flavor.Trim();
            juices = juices.Where(j => EF.Functions.ILike(j.Flavor, $"%{flavor}%"));
        }

        if (query.Featured.HasValue)
        {
            juices = juices.Where(j => j.IsFeatured == query.Featured.Value);
        }

        var totalCount = await juices.CountAsync(ct);
        var items = await juices
            .OrderByDescending(j => j.IsFeatured)
            .ThenBy(j => j.SortOrder)
            .ThenBy(j => j.Name)
            .Skip((pagination.PageNo - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .Select(j => new JuiceCatalogItemDto
            {
                Id = j.Id,
                Code = j.Code,
                Name = j.Name,
                Description = j.Description,
                Flavor = j.Flavor,
                VolumeMl = j.VolumeMl,
                Price = j.Price,
                ImageUrl = j.ImageUrl,
                IsFeatured = j.IsFeatured
            })
            .ToListAsync(ct);

        return new PagedResult<JuiceCatalogItemDto>
        {
            PageNo = pagination.PageNo,
            PageSize = pagination.PageSize,
            TotalCount = totalCount,
            Items = items
        };
    }

    public Task<JuiceCatalogItemDto?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        context.RawEntity<JuiceAggRoot>()
            .AsNoTracking()
            .Where(j => j.Id == id && j.Status == MasterDataStates.Enabled)
            .Select(j => new JuiceCatalogItemDto
            {
                Id = j.Id,
                Code = j.Code,
                Name = j.Name,
                Description = j.Description,
                Flavor = j.Flavor,
                VolumeMl = j.VolumeMl,
                Price = j.Price,
                ImageUrl = j.ImageUrl,
                IsFeatured = j.IsFeatured
            })
            .SingleOrDefaultAsync(ct);
}
