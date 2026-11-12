using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace Alcedo.Core.Presentation.Mvc.ViewModels;

public sealed class ErrorVm
{
    public int Status { get; set; }
    public string TraceId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Detail { get; set; }
    public string? Type { get; set; }
    public string? Instance { get; set; }

    public static ErrorVm FromProblemDetails(ProblemDetails pd, string traceId) => new()
    {
        Status = pd.Status ?? 500,
        Title = pd.Title ?? "Unhandled Exception",
        Detail = pd.Detail,
        Type = pd.Type,
        Instance = pd.Instance,
        TraceId = pd.Extensions.TryGetValue("traceId", out var t) && t is JsonElement je
            ? je.GetString() ?? traceId
            : traceId
    };
}