using System.Data;
using Alcedo.Core.Infrastructure.EfCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace Alcedo.Core.Infrastructure;

internal sealed class TransactionManager : IDisposable, IAsyncDisposable
{
    private readonly AlcedoDbContext _dbContext;
    private IDbContextTransaction? _transaction;
    private readonly ILogger<TransactionManager> _logger;
    private bool _owningTransaction = true;

    public TransactionManager(AlcedoDbContext dbContext, ILogger<TransactionManager> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger;
    }
    
    private bool CheckIfDbAlreadyBeginTransaction()
    {
        if (_dbContext.Database.CurrentTransaction != null)
        {
            _transaction = _dbContext.Database.CurrentTransaction;
            _owningTransaction = false;
            _logger.LogDebug("TxJoin: Joined an existing ambient transaction.");
            return true;
        }
        return false;
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (CheckIfDbAlreadyBeginTransaction())
        {
            return;
        }
        if (_transaction is null)
        {
            _transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            _logger.LogDebug("TxBegin:IsolationLevel=SystemDefault");
        }
    }

    public async Task BeginTransactionAsync(
        IsolationLevel isolationLevel,
        CancellationToken cancellationToken = default)
    {
        if (CheckIfDbAlreadyBeginTransaction())
        {
            return;
        }
        
        if (_transaction is null)
        {
            _transaction = await _dbContext.Database
                .BeginTransactionAsync(isolationLevel, cancellationToken);
            _logger.LogDebug("TxBegin:IsolationLevel={Level}", isolationLevel);
        }
    }

    public async Task<int> CommitAsync()
    {
        if (_transaction is null)
        {
            throw new InvalidOperationException("Can't Commit Since Transaction Doesn't Exist");
        }

        var result = await _dbContext.SaveChangesAsync();
        
        if (_owningTransaction)
        {
            await _transaction.CommitAsync();
        }
        
        await DisposeTransactionAsync();
        _logger.LogDebug("TxCommitted:EntitySaved={count}", result);
        
        return result;
    }

    public async Task RollbackAsync()
    {
        if (_transaction is null)
        {
            return;
        }
        
        try
        {
            if (_owningTransaction)
            {
                await _transaction.RollbackAsync();
                _logger.LogDebug("TxRollback");
            }
        }
        finally
        {
            _dbContext.ChangeTracker.Clear();
            await DisposeTransactionAsync();
        }
    }

    public async Task CreateSavepointAsync(string name, CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
        {
            throw new InvalidOperationException("Can't Create Save Point Since Transaction Doesn't Exist");
        }

        await _transaction.CreateSavepointAsync(name, cancellationToken);
    }

    public async Task RollbackToSavepointAsync(string name)
    {
        if (_transaction is null)
        {
            throw new InvalidOperationException("Can't Rollback Save Point Since Transaction Doesn't Exist");
        }

        await _transaction.RollbackToSavepointAsync(name);
    }

    public async Task ReleaseSavepointAsync(string name)
    {
        if (_transaction is null)
        {
            throw new InvalidOperationException("Can't Release Save Point Since Transaction Doesn't Exist");
        }

        await _transaction.ReleaseSavepointAsync(name);
    }

    private void DisposeTransaction()
    {
        if (_owningTransaction)
        {
            _transaction?.Dispose();
        }
        _transaction = null;
    }

    private async Task DisposeTransactionAsync()
    {
        if (_transaction is not null)
        {
            if (_owningTransaction)
            {
                await _transaction.DisposeAsync();
            }

            _transaction = null;
        }
    }

    public void Dispose()
    {
        DisposeTransaction();
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeTransactionAsync();
    }
}