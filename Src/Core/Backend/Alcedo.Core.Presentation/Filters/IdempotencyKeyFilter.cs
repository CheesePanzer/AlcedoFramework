using System.Security.Claims;
using Alcedo.Core.Application.Tools;
using Alcedo.Core.Presentation.PreException;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Alcedo.Core.Presentation.Filters;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class IgnoreIdempotencyKeyAttribute : Attribute;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class StrictIdempotencyKeyAttribute : Attribute;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class SoftIdempotencyKeyAttribute : Attribute;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class OverrideIdempotencyKeyExpiryMinutesAttribute : Attribute
{
    public int WindowMinutes { get; }
    public OverrideIdempotencyKeyExpiryMinutesAttribute(int windowMinutes) => WindowMinutes = windowMinutes;
}

public class RequestIdempotencyKeyFilter : IAsyncActionFilter
{
    private readonly IIdempotencyKeyProvider _factory;

    public RequestIdempotencyKeyFilter(IIdempotencyKeyProvider factory)
    {
        _factory = factory;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var request = context.HttpContext.Request;

        if (request.Method is "GET")
        {
            await next();
            return;
        }

        var metadata = context.ActionDescriptor.EndpointMetadata;
        if (metadata.OfType<IgnoreIdempotencyKeyAttribute>().Any())
        {
            await next();
            return;
        }

        var overridenExpMin = metadata.OfType<OverrideIdempotencyKeyExpiryMinutesAttribute>().FirstOrDefault()
            ?.WindowMinutes;
        var strictAttr = metadata.OfType<StrictIdempotencyKeyAttribute>().FirstOrDefault();
        var softKeyAttr = metadata.OfType<SoftIdempotencyKeyAttribute>().FirstOrDefault();

        var actionSign = GetActionSignature(context);

        var rawToken = request.Headers["X-Idempotency-Key"].ToString();

        var internalToken = AssembleInternalToken(context.HttpContext, rawToken);

        if (request.Method is "PUT" or "PATCH" or "DELETE" && strictAttr is null && softKeyAttr is null)
        {
            await next();
            return;
        }

        if (string.IsNullOrWhiteSpace(rawToken))
        {
            throw new PreErrIdempotencyKeyRequired();
        }

        if (strictAttr is not null)
        {
            if (!await _factory.ValidateAndConsumeStrictGeneratedIdempotencyKeyAsync(actionSign, internalToken))
            {
                throw new PreErrIdempotencyKeyExpired();
            }

            await next();
            return;
        }

        if (!await _factory.ValidateAndRegisterSoftIdempotencyKeyAsync(internalToken, actionSign, overridenExpMin))
        {
            throw new PreErrIdempotencyDuplicate();
        }

        await next();
    }

    /// <summary>
    /// Idempotency Token plus user id are considered a complete idempotency key
    /// </summary>
    private string AssembleInternalToken(HttpContext httpContext, string rawToken)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            return string.Empty;
        }

        var user = httpContext.User;
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? "anon";

        return $"{userId}:{rawToken}";
    }

    /// <summary>
    /// method and route template as the unique index for each action
    /// </summary>
    private string GetActionSignature(ActionExecutingContext context)
    {
        if (context.ActionDescriptor is ControllerActionDescriptor descriptor)
        {
            var method = context.HttpContext.Request.Method;

            var template = descriptor.AttributeRouteInfo?.Template
                           ?? $"{descriptor.ControllerName}/{descriptor.ActionName}";

            return $"{method}:{template}";
        }

        return context.HttpContext.Request.Path;
    }
}