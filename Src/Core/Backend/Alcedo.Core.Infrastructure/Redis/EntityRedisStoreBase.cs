using Alcedo.Core.Application;
using Alcedo.Core.Application.Dto;
using Microsoft.Extensions.Logging;

namespace Alcedo.Core.Infrastructure.Redis;

/// <summary>
/// Use this to auto register a keyspace into a EntityCacheRedisStore
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class RegisterAsEntityCacheStoreAttribute : Attribute
{
    public Type DtoType { get; }
    public RegisterAsEntityCacheStoreAttribute(Type dtoType) => DtoType = dtoType;
}

public class EntityCacheRedisStore<TKeySpace, TDto>
    : RedisJsonStoreBase<TKeySpace, TDto>, IEntityCacheStore<TDto>
    where TKeySpace : RedisKeySpaceBase
    where TDto : EntityCacheDtoBase
{
    public EntityCacheRedisStore(TKeySpace keySpace, ILogger<EntityCacheRedisStore<TKeySpace, TDto>> logger)
        : base(keySpace, logger)
    {
    }

    public Task SetAsync(TDto dto, TimeSpan expiry)
        => SetValueAsync(dto.Id.ToString(), dto, expiry);

    public Task<TDto?> GetByIdAsync(Guid id)
        => GetValueAsync(id.ToString());

    public Task DeleteByIdAsync(Guid id)
        => DeleteValueAsync(id.ToString());
}