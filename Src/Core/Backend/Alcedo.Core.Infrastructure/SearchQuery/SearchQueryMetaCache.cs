using System.Collections.Concurrent;
using System.Reflection;
using Alcedo.Core.Abstractions;
using Alcedo.Core.Application.SearchQuery;

namespace Alcedo.Core.Infrastructure.SearchQuery;

internal enum FilterKind
{
    Ignore,
    DateRange,
    Skip,
    GuidList,
    IntList,
    LongList,
    DecimalList,
    StringList,
    ValueAtomList,
    ValueAtom,
    String,
    Int,
    Long,
    Decimal,
    Bool,
    Guid,
    Enum,
    Unsupported
}

internal sealed record PropertyFilterMeta(
    PropertyInfo QueryProp,
    FilterKind Kind,
    DateRangeFilterAttribute? DateRange,
    string EntityPropertyName,
    Type UnderlyingEntityType,
    StringSearchMode? StringMode,
    Type? ElementType
);

internal static class SearchQueryMetaCache
{
    private static readonly ConcurrentDictionary<(Type, Type), PropertyFilterMeta[]> Cache = new();

    public static PropertyFilterMeta[] GetOrBuild(Type queryType, Type entityType)
        => Cache.GetOrAdd((queryType, entityType), static key =>
        {
            var (qt, et) = key;
            return qt.GetProperties().Select(prop => BuildMeta(prop, et)).ToArray();
        });

    private static Type? GetEnumerableElementType(Type type)
    {
        return type.GetInterfaces()
            .Concat([type])
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            ?.GetGenericArguments()[0];
    }

    private static PropertyFilterMeta BuildMeta(PropertyInfo prop, Type entityType)
    {
        if (prop.GetCustomAttribute<SearchQueryBuilderIgnoreAttribute>() is not null)
        {
            return new(prop, FilterKind.Ignore, null, prop.Name, typeof(object), null, null);
        }

        var dateRange = prop.GetCustomAttribute<DateRangeFilterAttribute>();
        if (dateRange is not null)
        {
            return new(prop, FilterKind.DateRange, dateRange, dateRange.EntityPropertyName, typeof(DateTimeOffset),
                null,
                null);
        }

        var filterField = prop.GetCustomAttribute<FilterFieldAttribute>();
        var entityPropName = filterField?.EntityPropertyName ?? prop.Name;
        var entityProp = entityType.GetProperty(entityPropName);

        if (entityProp is null)
        {
            return new(prop, FilterKind.Skip, null, entityPropName, typeof(object), null, null);
        }

        var stringMode = prop.GetCustomAttribute<StringSearchModeAttribute>()?.Mode;
        var underlying = Nullable.GetUnderlyingType(entityProp.PropertyType) ?? entityProp.PropertyType;
        var elementType = GetEnumerableElementType(prop.PropertyType);
        var kind = ResolveKind(underlying, elementType);

        return new(prop, kind, null, entityProp.Name, underlying, stringMode, elementType);
    }

    private static FilterKind ResolveKind(Type underlyingEntityType, Type? elementType)
    {
        if (elementType is not null)
        {
            if (elementType == typeof(Guid))
            {
                return FilterKind.GuidList;
            }

            if (elementType == typeof(int))
            {
                return FilterKind.IntList;
            }

            if (elementType == typeof(long))
            {
                return FilterKind.LongList;
            }

            if (elementType == typeof(decimal))
            {
                return FilterKind.DecimalList;
            }

            if (elementType == typeof(string))
            {
                return FilterKind.StringList;
            }

            if (typeof(ValueAtom).IsAssignableFrom(elementType))
            {
                return FilterKind.ValueAtomList;
            }
        }
        
        if (typeof(ValueAtom).IsAssignableFrom(underlyingEntityType))
        {
            return FilterKind.ValueAtom;
        }

        if (underlyingEntityType == typeof(string))
        {
            return FilterKind.String;
        }

        if (underlyingEntityType == typeof(int))
        {
            return FilterKind.Int;
        }

        if (underlyingEntityType == typeof(long))
        {
            return FilterKind.Long;
        }

        if (underlyingEntityType == typeof(decimal))
        {
            return FilterKind.Decimal;
        }

        if (underlyingEntityType == typeof(double))
        {
            throw new NotSupportedException("Double type not supported; Consider not to use double in your solution");
        }

        if (underlyingEntityType == typeof(bool))
        {
            return FilterKind.Bool;
        }

        if (underlyingEntityType == typeof(Guid))
        {
            return FilterKind.Guid;
        }

        if (underlyingEntityType.IsEnum)
        {
            return FilterKind.Enum;
        }
        
        return FilterKind.Unsupported;
    }
}