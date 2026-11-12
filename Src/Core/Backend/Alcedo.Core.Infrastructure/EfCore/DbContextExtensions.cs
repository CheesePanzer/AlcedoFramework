using System.Linq.Expressions;
using Alcedo.Core.Infrastructure.InfraException;
using Microsoft.EntityFrameworkCore;

namespace Alcedo.Core.Infrastructure.EfCore;

public static class DbContextModelExtensions
{
    public static string GetTableName<TEntity>(this DbContext context)
        where TEntity : class
    {
        var entityType = context.Model.FindEntityType(typeof(TEntity))
                         ?? throw new InfraErrEntityConfigMissing(typeof(TEntity).Name);

        var tableName = entityType.GetTableName()
                        ?? throw new InfraErrEntityConfigMissing(typeof(TEntity).Name);

        var schema = entityType.GetSchema();

        return schema is not null ? $"{schema}.{tableName}" : tableName;
    }

    public static string GetColumnName<TEntity, TProperty>(
        this DbContext context,
        Expression<Func<TEntity, TProperty>> propertyExpression)
        where TEntity : class
    {
        var memberExpression = (MemberExpression)propertyExpression.Body;
        var propertyName = memberExpression.Member.Name;

        return context.Model
                   .FindEntityType(typeof(TEntity))
                   ?.FindProperty(propertyName)
                   ?.GetColumnName()
               ?? throw new InfraErrEntityConfigMissing(propertyName);
    }
}