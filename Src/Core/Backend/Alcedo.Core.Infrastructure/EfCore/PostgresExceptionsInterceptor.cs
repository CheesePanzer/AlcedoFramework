using System.Data.Common;
using Alcedo.Core.Infrastructure.InfraException;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Alcedo.Core.Infrastructure.EfCore;

internal sealed class PostgresExceptionInterceptor : DbCommandInterceptor
{
    private readonly ILogger<PostgresExceptionInterceptor> _logger;

    public PostgresExceptionInterceptor(ILogger<PostgresExceptionInterceptor> logger)
    {
        _logger = logger;
    }

    public override void CommandFailed(DbCommand command, CommandErrorEventData data)
        => Translate(data.Exception);

    public override Task CommandFailedAsync(DbCommand command, CommandErrorEventData data,
        CancellationToken ct = default)
    {
        Translate(data.Exception);
        return Task.CompletedTask;
    }

    private void Translate(Exception exception)
    {
        if (exception is PostgresException pgEx)
        {
            _logger.LogError(pgEx,
                "Postgres exception translated: SqlState={SqlState}, Message={Message}",
                pgEx.SqlState, pgEx.Message);

            throw pgEx.SqlState switch
            {
                PostgresErrorCodes.LockNotAvailable => new InfraErrLockNotAvailable(),
                PostgresErrorCodes.DeadlockDetected => new InfraErrDeadlockDetected(),
                PostgresErrorCodes.QueryCanceled => new InfraErrLockTimeout(),
                PostgresErrorCodes.SerializationFailure => new InfraErrSerializationFailure(),
                _ => pgEx
            };
        }
    }
}