using Alcedo.Core.Application.Dto;

namespace Alcedo.Core.Application;

public interface IEntityCacheStore<TDto>
    where TDto : EntityCacheDtoBase
{
    Task SetAsync(TDto dto, TimeSpan expiry);
    Task<TDto?> GetByIdAsync(Guid id);
    Task DeleteByIdAsync(Guid id);
}