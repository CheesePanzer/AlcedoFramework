using System.Linq.Expressions;
using System.Reflection;
using Alcedo.Core.Abstractions;
using Alcedo.Core.Domain;
using Alcedo.Core.Domain.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Alcedo.Core.Infrastructure.Configuration;

public enum TrgmIndexType
{
    Gin,
    Gist
}

public static class EntityTypeBuilderExtensions
{
    public static PropertyBuilder<T> IsValueAtom<T>(
        this PropertyBuilder<T> builder,
        int maxLength = 50)
        where T : ValueAtom
    {
        return builder
            .HasConversion<ValueAtomConverter<T>>()
            .HasMaxLength(maxLength);
    }
    
    public static IndexBuilder<TEntity> HasTrgmIndex<TEntity>(
        this EntityTypeBuilder<TEntity> builder,
        Expression<Func<TEntity, object?>> expr,
        string? indexName = null,
        TrgmIndexType method = TrgmIndexType.Gin)
        where TEntity : EntityBase
    {
        if (string.IsNullOrWhiteSpace(indexName))
        {
            var tableName = builder.GetTableName();
            var propertyName = builder.GetColumnName(expr);
            indexName = $"ix_{tableName}_{propertyName}";
        }
        
        return builder
            .HasIndex(expr)
            .HasMethod(method.ToString().ToLowerInvariant())
            .HasOperators(method == TrgmIndexType.Gin ? "gin_trgm_ops" : "gist_trgm_ops")
            .HasDatabaseName(indexName);
    }
    
    private static string GetPropertyName<TEntity>(Expression<Func<TEntity, object?>> expression)
    {
        Expression body = expression.Body;
        
        if (body is UnaryExpression unaryExpression && unaryExpression.NodeType == ExpressionType.Convert)
        {
            body = unaryExpression.Operand;
        }

        if (body is MemberExpression memberExpression && memberExpression.Member is PropertyInfo propertyInfo)
        {
            return propertyInfo.Name;
        }

        throw new ArgumentException($"Cannot convert {expression} to trgm index name");
    }
    
    private static string GetColumnName<TEntity>(
        this EntityTypeBuilder<TEntity> builder,
        Expression<Func<TEntity, object?>> expr)
        where TEntity : EntityBase
    {
        var propertyName = GetPropertyName(expr);

        return builder.Metadata
                   .FindProperty(propertyName)
                   ?.GetColumnName()
               ?? throw new ArgumentException($"Cannot find column name for {propertyName}");
    }
    
    private static string GetTableName<TEntity>(
        this EntityTypeBuilder<TEntity> builder)
        where TEntity : EntityBase
    {
        return builder.Metadata.GetTableName() ?? throw new ArgumentException($"Cannot find table name for {typeof(TEntity).Name}");
    }
    
    /// <summary>
    /// Configures a collection of child entities within the same aggregate.
    ///
    /// Child entities share the lifecycle of the aggregate root:
    /// - Deleted automatically when the parent is deleted.
    /// - Accessed through the backing field to preserve encapsulation.
    /// - Optionally auto-included when loading the aggregate.
    ///
    /// This method uses HasMany instead of OwnsMany, allowing child entities
    /// to have their own identity while remaining part of the aggregate boundary.
    /// </summary>
    /// <typeparam name="TEntity">Aggregate root entity type.</typeparam>
    /// <typeparam name="TRelated">Child entity type.</typeparam>
    /// <param name="builder">Entity type builder.</param>
    /// <param name="navigationExpression">Navigation property to the child collection.</param>
    /// <param name="foreignKeyExpression">Foreign key property on the child entity.</param>
    /// <param name="autoInclude">
    /// Whether the child collection should be automatically included when loading the aggregate.
    /// Default is <c>true</c>.
    /// </param>
    /// <param name="fieldName">
    /// Backing field name for the navigation property. If omitted, a field name is inferred
    /// using the convention '_{camelCasePropertyName}'.
    /// </param>
    /// <returns>The same <see cref="EntityTypeBuilder{TEntity}"/> instance for chaining.</returns>
    public static EntityTypeBuilder<TEntity> HasChildEntities<TEntity, TRelated>(
        this EntityTypeBuilder<TEntity> builder,
        Expression<Func<TEntity, IEnumerable<TRelated>?>> navigationExpression,
        Expression<Func<TRelated, object?>> foreignKeyExpression,
        bool autoInclude = true,
        string? fieldName = null)
        where TEntity : EntityBase
        where TRelated : EntityBase
    {
        builder.HasMany(navigationExpression)
            .WithOne()
            .HasForeignKey(foreignKeyExpression)
            .OnDelete(DeleteBehavior.Cascade);

        if (string.IsNullOrEmpty(fieldName))
        {
            if (navigationExpression.Body is MemberExpression memberExpression &&
                memberExpression.Member is PropertyInfo propertyInfo)
            {
                string propertyName = propertyInfo.Name;
                fieldName = "_" + char.ToLowerInvariant(propertyName[0]) + propertyName.Substring(1);
            }
            else
            {
                throw new ArgumentException("Can't parse prop name into a valid field name, please check",
                    nameof(navigationExpression));
            }
        }

        if (autoInclude)
        {
            builder.Navigation(navigationExpression)
                .HasField(fieldName)
                .UsePropertyAccessMode(PropertyAccessMode.Field)
                .AutoInclude();
        }
        else
        {
            builder.Navigation(navigationExpression)
                .HasField(fieldName)
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        }

        return builder;
    }
    
    public static void ColumnIsNonNegative<TEntity>(
        this TableBuilder table,
        EntityTypeBuilder<TEntity> builder,
        Expression<Func<TEntity, object?>> expression)
        where TEntity : EntityBase
    {
        var tableName = builder.GetTableName();

        var columnName = builder.GetColumnName(expression);

        table.HasCheckConstraint(
            $"ck_{tableName}_{columnName.ToLowerInvariant()}_non_negative",
            $"\"{columnName}\" >= 0");
    }
    
    public static void ColumnIsPositive<TEntity>(
        this TableBuilder table,
        EntityTypeBuilder<TEntity> builder,
        Expression<Func<TEntity, object?>> expression)
        where TEntity : EntityBase
    {
        var tableName = builder.GetTableName();

        var columnName = builder.GetColumnName(expression);

        table.HasCheckConstraint(
            $"ck_{tableName}_{columnName.ToLowerInvariant()}_positive",
            $"\"{columnName}\" > 0");
    }
    
    public static void ColumnInRange<TEntity>(
        this TableBuilder table,
        EntityTypeBuilder<TEntity> builder,
        Expression<Func<TEntity, object?>> expression,
        decimal min,
        bool includeMin,
        decimal max,
        bool includeMax)
        where TEntity : EntityBase
    {
        var tableName = builder.GetTableName();

        var columnName = builder.GetColumnName(expression);

        var left = includeMin ? ">=" : ">";
        var right = includeMax ? "<=" : "<";

        table.HasCheckConstraint(
            $"ck_{tableName}_{columnName.ToLowerInvariant()}_range",
            $"\"{columnName}\" {left} {min} AND \"{columnName}\" {right} {max}");
    }
    
    public static void ColumnInOptions<TEntity>(
        this TableBuilder table,
        EntityTypeBuilder<TEntity> builder,
        string columnName,
        IReadOnlyCollection<string> values)
        where TEntity : EntityBase
    {
        if (values.Count == 0)
            throw new ArgumentException("Values cannot be empty.", nameof(values));

        var tableName = builder.GetTableName();
        
        var constraintName =
            $"ck_{tableName}_{columnName}_in_options";

        var sqlValues = string.Join(", ",
            values.Select(x => $"'{x.Replace("'", "''")}'"));

        table.HasCheckConstraint(
            constraintName,
            $"\"{columnName}\" IN ({sqlValues})");
    }
    
    public static void ColumnInOptions<TEntity>(
        this TableBuilder table,
        EntityTypeBuilder<TEntity> builder,
        Expression<Func<TEntity, object?>> expression,
        IReadOnlyCollection<string> values)
        where TEntity : EntityBase
    {
        table.ColumnInOptions(builder, builder.GetColumnName(expression), values);
    }
    
    public static void ColumnInStateValuesCollection<TEntity, TCollection>(
        this TableBuilder table,
        EntityTypeBuilder<TEntity> builder,
        Expression<Func<TEntity, object?>> expression)
        where TEntity : EntityBase
        where TCollection : IStateValuesCollection
    {
        table.ColumnInOptions(builder, expression, StateValuesCollectionReader.GetValues<TCollection>());
    }
    
    public static void ColumnInStateValuesCollection<TEntity, TCollection>(
        this TableBuilder table,
        EntityTypeBuilder<TEntity> builder,
        string columnName)
        where TEntity : EntityBase
        where TCollection : IStateValuesCollection
    {
        table.ColumnInOptions(builder, columnName, StateValuesCollectionReader.GetValues<TCollection>());
    }
}

public static class PropertyBuilderExtensions
{
    public static PropertyBuilder<TProperty> IsTextColumn<TProperty>(this PropertyBuilder<TProperty> propertyBuilder)
    {
        return propertyBuilder.HasColumnType("text");
    }

    public static PropertyBuilder<TProperty> IsJsonColumn<TProperty>(this PropertyBuilder<TProperty> propertyBuilder)
    {
        return propertyBuilder.HasColumnType("json");
    }

    public static PropertyBuilder<TProperty> IsMoneyColumn<TProperty>(this PropertyBuilder<TProperty> propertyBuilder,
        int precision = 18)
    {
        return propertyBuilder.HasPrecision(precision, 2);
    }

    public static PropertyBuilder<TProperty> IsJsonBColumn<TProperty>(this PropertyBuilder<TProperty> propertyBuilder)
    {
        return propertyBuilder.HasColumnType("jsonb");
    }
}