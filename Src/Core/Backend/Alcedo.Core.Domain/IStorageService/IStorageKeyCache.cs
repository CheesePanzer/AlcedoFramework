using System.Linq.Expressions;
using System.Reflection;
using Alcedo.Core.Domain.Entity;

namespace Alcedo.Core.Domain.IStorageService;

/// <summary>
/// 当对应的值被删除时，标识oss中的内部实体是否应该被删除
/// </summary>
public enum StorageKeyOnDelete
{
    Cascade,
    Ignore
}

public record StorageKeyProperty(PropertyInfo Property, string Bucket, StorageKeyOnDelete OnDelete);

public interface IStorageKeyCache
{
    bool HasStorageKeys(Type type);
    
    IReadOnlyList<StorageKeyProperty>? GetStorageKeys(Type type);
    
    string? GetBucket(Type entityType, PropertyInfo property);
    
    bool IsStorageKey(Type entityType, PropertyInfo property);
    
    string GetBucket<TEntity>(Expression<Func<TEntity, object?>> selector)
        where TEntity : EntityBase;
    
    string GetPublicUrl<TEntity>(
        Expression<Func<TEntity, object?>> selector,
        string objectName)
        where TEntity : EntityBase;

    string GetPublicUrl(
        Type entityType,
        PropertyInfo propType,
        string objectName);
}