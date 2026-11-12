using System.Linq.Expressions;
using Alcedo.Core.Infrastructure.EfCore;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Alcedo.Core.Infrastructure.RepositoryBase;

public sealed class PessimisticLockQueryBuilder<TEntity>
    where TEntity : class
{
    private readonly DbContext _context;
    private readonly DbSet<TEntity> _dbSet;
    private readonly string _tableName;
    private string _whereClause = "";
    private List<NpgsqlParameter> _parameters = [];
    private string _lockType = "";
    private string _waitStrategy = "";
    private bool _isReadOnly;

    public PessimisticLockQueryBuilder(DbContext context)
    {
        _context = context;
        _dbSet = context.Set<TEntity>();
        _tableName = context.GetTableName<TEntity>();
    }

    private PessimisticLockQueryBuilder<TEntity> Where(string condition, params NpgsqlParameter[] parameters)
    {
        _whereClause = string.IsNullOrEmpty(_whereClause)
            ? $"({condition})"
            : $"{_whereClause} AND ({condition})";
        _parameters.AddRange(parameters);
        return this;
    }

    private object? ConvertValue<TProperty>(string propertyName, TProperty value)
    {
        var converter = _context.Model
            .FindEntityType(typeof(TEntity))
            ?.FindProperty(propertyName)
            ?.GetValueConverter();

        return converter is not null ? converter.ConvertToProvider(value) : value;
    }

    public PessimisticLockQueryBuilder<TEntity> Where<TProperty>(
        Expression<Func<TEntity, TProperty>> propertyExpression,
        TProperty value)
    {
        var memberName = ((MemberExpression)propertyExpression.Body).Member.Name;
        var columnName = _context.GetColumnName(propertyExpression);
        var paramName = $"p{_parameters.Count}";

        return Where($"{columnName} = @{paramName}",
            new NpgsqlParameter(paramName, ConvertValue(memberName, value)));
    }

    public PessimisticLockQueryBuilder<TEntity> WhereIn<TProperty>(
        Expression<Func<TEntity, TProperty>> propertyExpression,
        IReadOnlyCollection<TProperty> values)
    {
        if (values.Count == 0)
        {
            return Where("1 = 0");
        }

        var memberName = ((MemberExpression)propertyExpression.Body).Member.Name;
        var columnName = _context.GetColumnName(propertyExpression);
        var parameters = values
            .Select((v, i) => new NpgsqlParameter($"p{_parameters.Count + i}", ConvertValue(memberName, v)))
            .ToArray();
        var inClause = string.Join(", ", parameters.Select(p => $"@{p.ParameterName}"));

        return Where($"{columnName} IN ({inClause})", parameters);
    }

    public PessimisticLockQueryBuilder<TEntity> ForUpdate()
    {
        _lockType = "FOR UPDATE";
        _isReadOnly = false;
        return this;
    }

    public PessimisticLockQueryBuilder<TEntity> ForShare()
    {
        _lockType = "FOR SHARE";
        _isReadOnly = true;
        return this;
    }

    public PessimisticLockQueryBuilder<TEntity> ForNoKeyUpdate()
    {
        _lockType = "FOR NO KEY UPDATE";
        _isReadOnly = false;
        return this;
    }

    public PessimisticLockQueryBuilder<TEntity> ForKeyShare()
    {
        _lockType = "FOR KEY SHARE";
        _isReadOnly = true;
        return this;
    }

    public PessimisticLockQueryBuilder<TEntity> NoWait()
    {
        _waitStrategy = "NOWAIT";
        return this;
    }

    public PessimisticLockQueryBuilder<TEntity> SkipLocked()
    {
        _waitStrategy = "SKIP LOCKED";
        return this;
    }

    public IQueryable<TEntity> BuildQuery()
    {
        var where = string.IsNullOrEmpty(_whereClause) ? "" : $"WHERE {_whereClause}";
        var dbLock = string.IsNullOrEmpty(_lockType) ? ""
            : string.IsNullOrEmpty(_waitStrategy) ? _lockType
            : $"{_lockType} {_waitStrategy}";

        var sql = $"""
                   SELECT * FROM {_tableName}
                   {where}
                   {dbLock}
                   """;

        var query = _parameters.Count > 0
            ? _dbSet.FromSqlRaw(sql, _parameters.Cast<object>().ToArray())
            : _dbSet.FromSqlRaw(sql);

        return _isReadOnly ? query.AsNoTracking() : query;
    }
}