using System.Linq.Expressions;
using System.Reflection;
using Alcedo.Core.Abstractions;
using Alcedo.Core.Application.SearchQuery;
using Alcedo.Core.Domain.Entity;
using Microsoft.EntityFrameworkCore;

namespace Alcedo.Core.Infrastructure.SearchQuery;

public static class SearchQueryBuilder
{
    public static Func<IQueryable<TEntity>, IQueryable<TEntity>> BuildFilter<TEntity, TSearch>(TSearch query)
        where TEntity : EntityBase
        where TSearch : EntitySearchQueryBase
        => BuildFilterInternal<TEntity, TSearch>(query, null);

    public static Func<IQueryable<TEntity>, IQueryable<TEntity>> BuildFilter<TEntity, TSearch>(
        TSearch query,
        params Expression<Func<TEntity, bool>>[] customPredicates)
        where TEntity : EntityBase
        where TSearch : EntitySearchQueryBase
        => BuildFilterInternal(query, customPredicates);

    #region Build Filter

    private static Func<IQueryable<TEntity>, IQueryable<TEntity>> BuildFilterInternal<TEntity, TSearch>(
        TSearch query,
        Expression<Func<TEntity, bool>>[]? customPredicates)
        where TEntity : EntityBase
        where TSearch : EntitySearchQueryBase
    {
        var metas = SearchQueryMetaCache.GetOrBuild(typeof(TSearch), typeof(TEntity));

        return q =>
        {
            if (customPredicates is { Length: > 0 })
            {
                foreach (var predicate in customPredicates)
                {
                    q = q.Where(predicate);
                }
            }

            foreach (var meta in metas)
            {
                switch (meta.Kind)
                {
                    case FilterKind.Ignore:
                    case FilterKind.Skip:
                    case FilterKind.Unsupported:
                        continue;

                    case FilterKind.DateRange:
                    {
                        var value = meta.QueryProp.GetValue(query);
                        if (value is null)
                        {
                            continue;
                        }

                        var dt = (DateTimeOffset)value;
                        q = meta.DateRange!.Bound == DateRangeBound.Start
                            ? q.Where(e => EF.Property<DateTimeOffset>(e, meta.EntityPropertyName) >= dt)
                            : q.Where(e => EF.Property<DateTimeOffset>(e, meta.EntityPropertyName) < dt);
                        break;
                    }

                    default:
                    {
                        var value = meta.QueryProp.GetValue(query);
                        if (value is null)
                        {
                            continue;
                        }

                        q = ApplyFilter(q, meta, value);
                        break;
                    }
                }
            }

            return q;
        };
    }

    private static IQueryable<TEntity> ApplyFilter<TEntity>(
        IQueryable<TEntity> q,
        PropertyFilterMeta meta,
        object value)
        where TEntity : EntityBase
    {
        var name = meta.EntityPropertyName;
        var underlying = meta.UnderlyingEntityType;

        return meta.Kind switch
        {
            FilterKind.GuidList => ApplyListFilter<TEntity, Guid>(q, name, value),
            FilterKind.IntList => ApplyListFilter<TEntity, int>(q, name, value),
            FilterKind.LongList => ApplyListFilter<TEntity, long>(q, name, value),
            FilterKind.DecimalList => ApplyListFilter<TEntity, decimal>(q, name, value),
            FilterKind.StringList => ApplyListFilter<TEntity, string>(q, name, value),

            FilterKind.ValueAtomList => ApplyValueAtomListFilter(q, meta, value),
            FilterKind.ValueAtom => ApplyValueAtomFilter(q, name, underlying, value),

            FilterKind.String => ApplyStringFilter(q, name, value, meta.StringMode),
            FilterKind.Int => q.Where(e => EF.Property<int>(e, name) == Convert.ToInt32(value)),
            FilterKind.Long => q.Where(e => EF.Property<long>(e, name) == Convert.ToInt64(value)),
            FilterKind.Decimal => q.Where(e => EF.Property<decimal>(e, name) == Convert.ToDecimal(value)),
            FilterKind.Bool => q.Where(e => EF.Property<bool>(e, name) == Convert.ToBoolean(value)),
            FilterKind.Guid => q.Where(e => EF.Property<Guid>(e, name) == Guid.Parse(value.ToString()!)),
            FilterKind.Enum => q.Where(e => EF.Property<int>(e, name) == Convert.ToInt32(value)),

            _ => q
        };
    }

    private static IQueryable<TEntity> ApplyListFilter<TEntity, TValue>(
        IQueryable<TEntity> q, string name, object value)
        where TEntity : EntityBase
    {
        if (value is not IEnumerable<TValue> enumerable)
        {
            return q;
        }

        var list = enumerable.ToList();
        return list.Count > 0
            ? q.Where(e => list.Contains(EF.Property<TValue>(e, name)))
            : q;
    }

    private static IQueryable<TEntity> ApplyStringFilter<TEntity>(
        IQueryable<TEntity> q, string name, object value, StringSearchMode? mode)
        where TEntity : EntityBase
    {
        var v = Convert.ToString(value);
        if (string.IsNullOrWhiteSpace(v))
        {
            return q;
        }

        return mode switch
        {
            StringSearchMode.Exact => q.Where(e => EF.Property<string>(e, name) == v),
            StringSearchMode.StartsWith => q.Where(e => EF.Property<string>(e, name).StartsWith(v)),
            StringSearchMode.EndsWith => q.Where(e => EF.Property<string>(e, name).EndsWith(v)),
            StringSearchMode.Contains => q.Where(e => EF.Property<string>(e, name).Contains(v)),
            _ => q.Where(e => EF.Property<string>(e, name).StartsWith(v))
        };
    }

    private static IQueryable<TEntity> ApplyDoubleFilter<TEntity>(
        IQueryable<TEntity> q, string name, object value)
        where TEntity : EntityBase
    {
        throw new NotSupportedException("may be you shouldn't use double in your solution");
    }

    private static IQueryable<TEntity> ApplyValueAtomFilter<TEntity>(
        IQueryable<TEntity> q, string name, Type underlying, object value)
        where TEntity : EntityBase
    {
        if (typeof(ValueAtom).IsAssignableFrom(underlying))
        {
            return q.Where(e =>
                EF.Property<object>(e, name).Equals(value));
        }

        var atomValue = Activator.CreateInstance(underlying, value.ToString())!;
        return q.Where(e =>
            EF.Property<object>(e, name).Equals(atomValue));
    }

    private static IQueryable<TEntity> ApplyValueAtomListFilter<TEntity>(
        IQueryable<TEntity> q, PropertyFilterMeta meta, object value)
        where TEntity : EntityBase
    {
        var method = typeof(SearchQueryBuilder)
            .GetMethod(nameof(ApplyValueAtomListFilterGeneric), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(typeof(TEntity), meta.ElementType!);
        
        var castMethod = typeof(Enumerable)
            .GetMethod(nameof(Enumerable.Cast))!
            .MakeGenericMethod(meta.ElementType!);
    
        var castedList = castMethod.Invoke(null, [value]);
    
        var toListMethod = typeof(Enumerable)
            .GetMethod(nameof(Enumerable.ToList))!
            .MakeGenericMethod(meta.ElementType!);
    
        var typedList = toListMethod.Invoke(null, [castedList]);

        return (IQueryable<TEntity>)method.Invoke(null, [q, meta.EntityPropertyName, typedList])!;
    }

    private static IQueryable<TEntity> ApplyValueAtomListFilterGeneric<TEntity, TAtom>(
        IQueryable<TEntity> q, string name, List<TAtom> list)
        where TEntity : EntityBase
        where TAtom : ValueAtom
    {
        return list.Count > 0
            ? q.Where(e => list.Contains(EF.Property<TAtom>(e, name)))
            : q;
    }

    #endregion

    #region Build Order By

    public static Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? BuildOrderBy<TEntity>(SortQuery sort)
        where TEntity : EntityBase
    {
        if (!sort.HasSort)
        {
            return null;
        }

        var entityType = typeof(TEntity);

        return q =>
        {
            IOrderedQueryable<TEntity>? ordered = null;

            bool hasIdField = false;

            foreach (var sortField in sort.Fields!)
            {
                var prop = entityType.GetProperty(sortField.Field);
                if (prop is null)
                {
                    throw new ArgumentException(
                        $"Entity '{entityType.Name}' does not have a property '{sortField.Field}'.");
                }

                var fieldName = prop.Name;

                if (fieldName == nameof(EntityBase.Id))
                {
                    hasIdField = true;
                }

                ordered = ordered is null
                    ? sortField.Descending
                        ? q.OrderByDescending(e => EF.Property<object>(e, fieldName))
                        : q.OrderBy(e => EF.Property<object>(e, fieldName))
                    : sortField.Descending
                        ? ordered.ThenByDescending(e => EF.Property<object>(e, fieldName))
                        : ordered.ThenBy(e => EF.Property<object>(e, fieldName));
            }

            return hasIdField ? ordered! : ordered!.ThenBy(e => e.Id);
        };
    }

    public static Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> BuildOrderByOrDefault<TEntity>(SortQuery sort)
        where TEntity : EntityBase
    {
        return BuildOrderBy<TEntity>(sort) ?? (q => q.OrderBy(e => e.Id));
    }

    #endregion
}