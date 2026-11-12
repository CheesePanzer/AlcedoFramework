using System.Text.Json.Serialization;
using Alcedo.Core.Domain.Entity;

namespace Alcedo.Framework.Admin.Domain.Entity;

public sealed class AccessLogEntity : EntityBase
{
    public AccessLogEntity(
        string? traceId,
        string ipAddress,
        string? userAgent,
        string method,
        string? area,
        string controller,
        string action,
        string url,
        DateTimeOffset requestedAt,
        int? statusCode,
        int? executionMs,
        string? exception)
    {
        TraceId = traceId;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        Method = method;
        Area = area;
        Controller = controller;
        Action = action;
        Url = url;
        RequestedAt = requestedAt;
        StatusCode = statusCode;
        ExecutionMs = executionMs;
        Exception = exception;
    }

    public string? TraceId { get; set; }

    public string IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public string Method { get; set; }

    public string? Area { get; set; }

    public string Controller { get; set; }

    public string Action { get; set; }

    public string Url { get; set; }

    public DateTimeOffset RequestedAt { get; set; }

    public int? StatusCode { get; set; }

    public int? ExecutionMs { get; set; }

    public string? Exception { get; set; }

    [JsonConstructor]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    private AccessLogEntity()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    {
    }
}