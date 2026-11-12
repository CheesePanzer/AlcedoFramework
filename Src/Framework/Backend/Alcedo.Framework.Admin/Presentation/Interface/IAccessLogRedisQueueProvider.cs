using Alcedo.Framework.Admin.Domain.Entity;

namespace Alcedo.Framework.Admin.Presentation.Interface;

public interface IAccessLogRedisStore
{
    Task EnqueueLogAsync(AccessLogEntity log);

    Task<IReadOnlyList<string>> DequeueRawLogsAsync(int batchSize);

    Task RequeueRawLogsAsync(IEnumerable<string> logs);
}