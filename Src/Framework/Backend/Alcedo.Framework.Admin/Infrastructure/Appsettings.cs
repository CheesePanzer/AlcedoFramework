using Alcedo.Core.Abstractions;

namespace Alcedo.Framework.Admin.Infrastructure;

public sealed class AccessLogOptions : AppSettingsBase
{
    [AppSettingsDemo("500", "Number of access log entries to dequeue and persist per batch job execution.")]
    public int BatchFetchSize { get; set; }
}