using Alcedo.Core.Application.Dto;
using Alcedo.Core.Domain.DomException;
using Alcedo.Core.Domain.Entity;
using Alcedo.Core.Domain.IRepositoryBase;
using MapsterMapper;

namespace Alcedo.Core.Application;

public interface IEntityCacheProvider<TDto> where TDto : EntityCacheDtoBase
{
    Task<TDto> GetOrFallbackDbAsync(Guid id, CancellationToken ct = default);
    Task<TDto> RefreshFromDbAsync(Guid id, TimeSpan? expiry = null, CancellationToken ct = default);
    Task SetAsync(TDto dto, TimeSpan? expiry = null);
    Task DeleteAsync(Guid id);
}

public abstract class EntityCacheProvider<TAgg, TDto> : IEntityCacheProvider<TDto> where TAgg : EntityBase
    where TDto : EntityCacheDtoBase
{
    protected readonly IEntityCacheStore<TDto> CacheStore;
    protected readonly IEntityStoreBase<TAgg> DbStore;
    protected readonly IMapper Mapper;
    protected readonly TimeSpan DefaultExpiry;

    protected EntityCacheProvider(IEntityCacheStore<TDto> cacheStore, TimeSpan defaultExpiry,
        IEntityStoreBase<TAgg> dbStore, IMapper mapper)
    {
        CacheStore = cacheStore;
        DefaultExpiry = defaultExpiry;
        DbStore = dbStore;
        Mapper = mapper;
    }

    protected virtual async Task<TDto> FetchFromDbAsync(Guid id, CancellationToken ct)
    {
        var agg = await DbStore.GetByIdAsync(id, ct)
                  ?? throw new DomErrResourceNotFound<TAgg>(id);
        return Mapper.Map<TDto>(agg);
    }

    public async Task<TDto> GetOrFallbackDbAsync(Guid id, CancellationToken ct = default)
    {
        var cached = await CacheStore.GetByIdAsync(id);
        if (cached is not null)
        {
            return cached;
        }

        return await RefreshFromDbAsync(id, ct: ct);
    }

    public async Task<TDto> RefreshFromDbAsync(Guid id, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        var dto = await FetchFromDbAsync(id, ct);
        await CacheStore.SetAsync(dto, expiry ?? DefaultExpiry);
        return dto;
    }

    public Task SetAsync(TDto dto, TimeSpan? expiry = null)
        => CacheStore.SetAsync(dto, expiry ?? DefaultExpiry);

    public Task DeleteAsync(Guid id)
        => CacheStore.DeleteByIdAsync(id);
}