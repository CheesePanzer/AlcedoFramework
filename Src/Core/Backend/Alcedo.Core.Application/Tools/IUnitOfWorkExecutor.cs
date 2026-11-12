namespace Alcedo.Core.Application.Tools;

public interface IUnitOfWorkExecutor
{
    #region ExecuteAsync No Retry

    Task ExecuteAsync(Action action, CancellationToken ct = default);

    Task ExecuteAsync(Func<Task> action, CancellationToken ct = default);

    Task ExecuteAsync(Func<CancellationToken, Task> action, CancellationToken ct = default);

    Task<T> ExecuteAsync<T>(Func<T> action, CancellationToken ct = default);

    Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken ct = default);

    Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct = default);

    #endregion

    #region ExecuteAsync With Retry

    Task ExecuteWithRetryAsync(Action action, int maxRetries = 3, CancellationToken ct = default);

    Task ExecuteWithRetryAsync(Func<Task> action, int maxRetries = 3, CancellationToken ct = default);

    Task ExecuteWithRetryAsync(Func<CancellationToken, Task> action, int maxRetries = 3,
        CancellationToken ct = default);

    Task<T> ExecuteWithRetryAsync<T>(Func<T> action, int maxRetries = 3, CancellationToken ct = default);

    Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> action, int maxRetries = 3, CancellationToken ct = default);

    Task<T> ExecuteWithRetryAsync<T>(Func<CancellationToken, Task<T>> action, int maxRetries = 3,
        CancellationToken ct = default);

    #endregion

    #region Save Point Mgt

    Task CreateSavepointAsync(string name, CancellationToken cancellationToken = default);

    Task RollbackToSavepointAsync(string name);

    Task ReleaseSavepointAsync(string name);

    #endregion

    void AddApplicationEvent(ApplicationEventBase appEvent);

    void AddApplicationEvents(IEnumerable<ApplicationEventBase> appEvent);
}