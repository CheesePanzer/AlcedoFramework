using System.Linq.Expressions;
using System.Reflection;
using Alcedo.Core.Domain.Entity;
using Alcedo.Core.Domain.IStorageService;
using Alcedo.Core.Infrastructure.InfraException;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Options;

namespace Alcedo.Core.Infrastructure.Storage;

internal static class StorageKeyCacheData
{
    public static IReadOnlyDictionary<Type, IReadOnlyList<StorageKeyProperty>> Cache
        = new Dictionary<Type, IReadOnlyList<StorageKeyProperty>>();
    
    public static void Initialize(IModel model)
    {
        var result = new Dictionary<Type, IReadOnlyList<StorageKeyProperty>>();

        foreach (var entityType in model.GetEntityTypes())
        {
            var props = entityType.GetProperties()
                .Where(p => p.IsStorageKey())
                .Select(p => new StorageKeyProperty(p.PropertyInfo!, p.GetStorageBucket(), p.GetStorageKeyOnDelete()))
                .ToList();

            if (props.Count > 0)
            {
                result[entityType.ClrType] = props;
            }
        }

        Cache = result;
    }
}

internal class StorageKeyCache : IStorageKeyCache
{
    private readonly StorageOptions _storageOptions;
    
    public StorageKeyCache(IOptions<StorageOptions> storageOptions)
    {
        _storageOptions = storageOptions.Value;
    }
    
    public bool HasStorageKeys(Type type)
        => StorageKeyCacheData.Cache.ContainsKey(type);

    public IReadOnlyList<StorageKeyProperty>? GetStorageKeys(Type type)
        => StorageKeyCacheData.Cache.GetValueOrDefault(type);

    public string? GetBucket(Type entityType, PropertyInfo property)
        => StorageKeyCacheData.Cache.GetValueOrDefault(entityType)
            ?.FirstOrDefault(p => p.Property == property)
            ?.Bucket;

    public bool IsStorageKey(Type entityType, PropertyInfo property)
        => StorageKeyCacheData.Cache.GetValueOrDefault(entityType)
            ?.Any(p => p.Property == property) ?? false;
    
    public string GetBucket<TEntity>(Expression<Func<TEntity, object?>> selector)
        where TEntity : EntityBase
    {
        var body = selector.Body is UnaryExpression unary ? unary.Operand : selector.Body;
        var prop = (PropertyInfo)((MemberExpression)body).Member;
        return GetBucket(typeof(TEntity), prop) ?? throw new InfraErrColumnNotStorageKey();
    }
    
    public string GetPublicUrl<TEntity>(
        Expression<Func<TEntity, object?>> selector,
        string objectName)
        where TEntity : EntityBase
    {
        var bucket = GetBucket(selector);
        var baseUrl = _storageOptions.S3PublicUrl.TrimEnd('/');
        return $"{baseUrl}/{bucket}/{objectName}";
    }
    
    public string GetPublicUrl(
        Type entityType,
        PropertyInfo propInfo,
        string objectName)
    {
        var bucket = GetBucket(entityType, propInfo) ?? throw new InfraErrColumnNotStorageKey();
        var baseUrl = _storageOptions.S3PublicUrl.TrimEnd('/');
        return $"{baseUrl}/{bucket}/{objectName}";
    }
}