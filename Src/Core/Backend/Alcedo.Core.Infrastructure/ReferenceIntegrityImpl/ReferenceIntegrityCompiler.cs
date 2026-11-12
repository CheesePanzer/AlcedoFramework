using System.Linq.Expressions;
using System.Reflection;
using Alcedo.Core.Domain.ReferenceIntegrity;
using Alcedo.Core.Infrastructure.EfCore;
using Microsoft.EntityFrameworkCore;

namespace Alcedo.Core.Infrastructure.ReferenceIntegrityImpl;

internal static class ReferenceIntegrityCompiler
{
    /// <summary>
    /// Compiles a dynamic check: 
    /// (context, id, ct) => context.Set{TEntity}().AnyAsync(e => e.Prop1 == id || e.Prop2 == id, ct)
    /// </summary>
    public static Func<AlcedoDbContext, Guid, CancellationToken, Task<bool>> BuildCheckReferenceFunc(
        Type entityType,
        List<string> propertyNames)
    {
        var contextParam = Expression.Parameter(typeof(AlcedoDbContext), "context");
        var idParam = Expression.Parameter(typeof(Guid), "id");
        var ctParam = Expression.Parameter(typeof(CancellationToken), "ct");
        var itemParam = Expression.Parameter(entityType, "e");

        // Construct OR conditions: (e.Prop1 == id || e.Prop2 == id)
        var equalityExpressions = propertyNames.Select(propName =>
        {
            var propertyAccess = Expression.Property(itemParam, propName);
            var propType = ((PropertyInfo)propertyAccess.Member).PropertyType;

            Expression idExpr = idParam;
            if (propType != typeof(Guid))
            {
                idExpr = Expression.Convert(idParam, propType);
            }

            return Expression.Equal(propertyAccess, idExpr);
        }).ToList();

        if (equalityExpressions.Count == 0)
        {
            return (ctx, id, ct) => Task.FromResult(false);
        }

        Expression body = equalityExpressions.Aggregate(Expression.OrElse);
        var predicate = Expression.Lambda(body, itemParam);

        var setMethod =
            typeof(DbContext).GetMethod(nameof(DbContext.Set), 1, Type.EmptyTypes)!.MakeGenericMethod(entityType);
        var setCall = Expression.Call(contextParam, setMethod);

        var anyAsyncMethod = typeof(EntityFrameworkQueryableExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .First(m => m.Name == nameof(EntityFrameworkQueryableExtensions.AnyAsync) && m.GetParameters().Length == 3)
            .MakeGenericMethod(entityType);

        var anyCall = Expression.Call(anyAsyncMethod, setCall, predicate, ctParam);

        return Expression.Lambda<Func<AlcedoDbContext, Guid, CancellationToken, Task<bool>>>(
            anyCall,
            contextParam,
            idParam,
            ctParam
        ).Compile();
    }

    public static Func<AlcedoDbContext, Guid, CancellationToken, Task<bool>> MergeCheckFuncs(
        IEnumerable<Func<AlcedoDbContext, Guid, CancellationToken, Task<bool>>> funcs)
    {
        var funcList = funcs.ToList();
        return async (ctx, id, ct) =>
        {
            foreach (var func in funcList)
            {
                if (await func(ctx, id, ct))
                {
                    return true;
                }
            }

            return false;
        };
    }

    public static Func<object, object?> CompileNavGetter(Type type, string propName)
    {
        var param = Expression.Parameter(typeof(object), "obj");

        var cast = Expression.Convert(param, type);

        var prop = Expression.Property(cast, propName);

        var convert = Expression.Convert(prop, typeof(object));

        return Expression.Lambda<Func<object, object?>>(convert, param).Compile();
    }

    private static void AddToResult(Dictionary<Type, HashSet<Guid>> result, Type key, Guid id)
    {
        if (!result.TryGetValue(key, out var set))
        {
            set = new HashSet<Guid>();
            result[key] = set;
        }

        set.Add(id);
    }

    /// <summary>
    /// Compile a func that can extract dic(type, ids) from an entity
    /// </summary>
    public static Action<object, Dictionary<Type, HashSet<Guid>>> CompileIdExtractor(
        Type entityType,
        IEnumerable<ReferenceConfiguration> typeConfigs)
    {
        var objParam = Expression.Parameter(typeof(object), "obj");
        var dictParam = Expression.Parameter(typeof(Dictionary<Type, HashSet<Guid>>), "dict");
        var castedObj = Expression.Convert(objParam, entityType);

        var bodyExpressions = new List<Expression>();

        foreach (var config in typeConfigs)
        {
            var propInfo = entityType.GetProperty(config.ForeignKeyProperty)!;
            var propExpr = Expression.Property(castedObj, propInfo);
            var principalType = Expression.Constant(config.PrincipalAggregateType);

            var method = typeof(ReferenceIntegrityCompiler)
                .GetMethod(nameof(AddToResult), BindingFlags.NonPublic | BindingFlags.Static)!;

            Expression guidValueExpr;
            Expression conditionExpr;

            if (Nullable.GetUnderlyingType(propInfo.PropertyType) is not null)
            {
                // For nullable Guid: if (prop.HasValue && prop.Value != Guid.Empty)
                var hasValueProp = Expression.Property(propExpr, "HasValue");
                var valueProp = Expression.Property(propExpr, "Value");

                guidValueExpr = valueProp;
                conditionExpr = Expression.AndAlso(
                    hasValueProp,
                    Expression.NotEqual(valueProp, Expression.Constant(Guid.Empty))
                );
            }
            else
            {
                // if (prop != Guid.Empty)
                guidValueExpr = propExpr;
                conditionExpr = Expression.NotEqual(propExpr, Expression.Constant(Guid.Empty));
            }

            var callAdd = Expression.Call(
                null,
                method,
                dictParam,
                principalType,
                guidValueExpr);

            bodyExpressions.Add(Expression.IfThen(conditionExpr, callAdd));
        }

        if (bodyExpressions.Count == 0)
        {
            return (o, d) => { };
        }

        var block = Expression.Block(bodyExpressions);
        return Expression.Lambda<Action<object, Dictionary<Type, HashSet<Guid>>>>(block, objParam, dictParam).Compile();
    }
}