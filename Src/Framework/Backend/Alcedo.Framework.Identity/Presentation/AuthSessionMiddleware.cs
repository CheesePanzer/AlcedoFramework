using System.Security.Claims;
using Alcedo.Core.Application;
using Alcedo.Core.Domain.DomException;
using Alcedo.Framework.Identity.Application.Dto.User;
using Alcedo.Framework.Identity.Contracts;
using Alcedo.Framework.Identity.Domain.Entity;
using Alcedo.Framework.Identity.Exceptions;
using Alcedo.Framework.Shared;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Alcedo.Framework.Identity.Presentation;

/// <summary>
/// Global Login Validation And Session Validation
/// </summary>
public sealed class AuthSessionMiddleware
{
    private readonly RequestDelegate _next;

    public AuthSessionMiddleware(
        RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IEntityCacheProvider<UserCacheDto> provider,
        ILogger<AuthSessionMiddleware> logger)
    {
        var ct = context.RequestAborted;

        var endpoint = context.GetEndpoint();

        if (endpoint?.Metadata.GetMetadata<IAllowAnonymous>() is not null)
        {
            await _next(context);
            return;
        }
        
        if (context.User.Identities.All(i => i.AuthenticationType != AlcedoIdentityScheme.AuthenticationScheme))
        {
            await _next(context);
            return;
        }

        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            logger.LogError("No authenticated user enter AuthSessionMiddleware");
            await context.ChallengeAsync();
            return;
        }

        var userIdString = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        var sessionParseSuccess =
            Guid.TryParse(context.User.FindFirstValue(AlcedoClaimNames.CurrentSession), out Guid sessionId);

        if (string.IsNullOrEmpty(userIdString) || !sessionParseSuccess)
        {
            logger.LogError("Missing UserId or SessionId in context");
            await context.ChallengeAsync();
            return;
        }

        if (!Guid.TryParse(userIdString, out var userId))
        {
            logger.LogError("UserId Incorrect format: {id}", userIdString);
            await context.ChallengeAsync();
            return;
        }

        UserCacheDto cachedUser;
        try
        {
            cachedUser = await provider.GetOrFallbackDbAsync(userId, ct);
        }
        catch (DomErrResourceNotFound<UserAgg>)
        {
            logger.LogWarning(
                "User {UserId} not found but valid JWT still present", userId);
            throw new IdentityErrUnauthenticated();
        }

        if (!cachedUser.CanAccessContext)
        {
            throw new IdentityErrInvalidUser($"User {userId} is locked");
        }

        if (cachedUser.CurrentSession is null || sessionId != cachedUser.CurrentSession)
        {
            throw new IdentityErrSessionInvalid(userId, cachedUser.CurrentSession);
        }

        context.Items[HttpContextUserStoreKey.Key] = cachedUser;

        await _next(context);
    }
}