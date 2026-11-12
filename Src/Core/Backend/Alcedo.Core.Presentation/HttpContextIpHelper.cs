using Microsoft.AspNetCore.Http;

namespace Alcedo.Core.Presentation;

public static class HttpContextIpHelper
{
    public static string GetClientIp(HttpContext context) =>
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}