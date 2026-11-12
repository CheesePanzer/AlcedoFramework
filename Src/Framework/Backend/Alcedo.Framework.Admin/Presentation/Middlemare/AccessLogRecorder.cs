using Alcedo.Core.Presentation;
using Alcedo.Framework.Admin.Domain.Entity;
using Alcedo.Framework.Admin.Presentation.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Alcedo.Framework.Admin.Presentation.Middlemare;
[Obsolete("Use Serilog Request Logging instead")]
internal class AccessLogRecorderMiddleware
{
    private readonly RequestDelegate _next;

    public AccessLogRecorderMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var request = context.Request;
        var method = request.Method;
        var url = $"{request.Scheme}://{request.Host}{request.Path}{request.QueryString}";
        var controller = context.GetRouteValue("controller")?.ToString() ?? "";
        var action = context.GetRouteValue("action")?.ToString() ?? "";
        var area = context.GetRouteValue("area")?.ToString();
        var traceId = context.TraceIdentifier;
        var ipAddress = HttpContextIpHelper.GetClientIp(context);
        var userAgent = request.Headers["User-Agent"].ToString();
        var requestedAt = DateTimeOffset.UtcNow;

        string? exceptionMessage = null;
        var startMs = Environment.TickCount64;

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            exceptionMessage = ex.Message;
            throw;
        }
        finally
        {
            var executionMs = (int)(Environment.TickCount64 - startMs);

            var log = new AccessLogEntity(
                traceId: traceId,
                ipAddress: ipAddress,
                userAgent: userAgent,
                method: method,
                area: area,
                controller: controller,
                action: action,
                url: url,
                requestedAt: requestedAt,
                statusCode: context.Response.StatusCode,
                executionMs: executionMs,
                exception: exceptionMessage
            );

            var logQueue = context.RequestServices.GetRequiredService<IAccessLogRedisStore>();
            await logQueue.EnqueueLogAsync(log);
        }
    }
}