using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Alcedo.Core.Application;
using Alcedo.Core.Presentation;
using Alcedo.Framework.Identity.Application.Dto.User;
using Alcedo.Framework.Identity.Application.Provider;
using Alcedo.Framework.Identity.Contracts;
using Alcedo.Framework.Identity.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace Alcedo.Framework.Identity.Presentation;

public sealed class AlcedoRbacHandler : AuthorizationHandler<AlcedoPermissionRequirement>
{
    private readonly IEntityCacheProvider<UserCacheDto> _userCacheProvider;
    private readonly IRoleCacheProvider _roleCache;
    private readonly EndpointResourceOperationCenter _center;
    private readonly ILogger<AlcedoRbacHandler> _logger;

    public AlcedoRbacHandler(
        IEntityCacheProvider<UserCacheDto> userCacheProvider,
        IRoleCacheProvider roleCache,
        EndpointResourceOperationCenter center, ILogger<AlcedoRbacHandler> logger)
    {
        _userCacheProvider = userCacheProvider;
        _roleCache = roleCache;
        _center = center;
        _logger = logger;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AlcedoPermissionRequirement requirement)
    {
        var httpContext = context.Resource as HttpContext
                          ?? (context.Resource as AuthorizationFilterContext)?.HttpContext;

        if (httpContext is null)
        {
            _logger.LogWarning("AuthorizationFilterContext was null.");
            context.Fail();
            return;
        }

        var ct = httpContext.RequestAborted;

        var endpoint = httpContext.GetEndpoint();

        if (endpoint?.Metadata.GetMetadata<IAllowAnonymous>() is not null ||
            endpoint?.Metadata.GetMetadata<ISkipPermissionCheck>() is not null)
        {
            context.Succeed(requirement);
            return;
        }
        
        if (context.User?.Identity?.IsAuthenticated != true)
        {
            _logger.LogWarning("User is not authenticated. RequireAuthenticatedUser handle challenge.");
            return; 
        }

        UserCacheDto? cachedUser;

        if (httpContext.Items.TryGetValue(HttpContextUserStoreKey.Key, out var cachedUserObj))
        {
            cachedUser = cachedUserObj as UserCacheDto
                         ?? throw new ValidationException("CachedUser wrong type");
        }
        else
        {
            var userIdStr = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!Guid.TryParse(userIdStr, out var userId))
            {
                _logger.LogWarning("UserId incorrect format {userid}", userIdStr);
                context.Fail();
                return;
            }

            cachedUser = await _userCacheProvider.GetOrFallbackDbAsync(userId, ct);
        }

        if (!cachedUser.CanAccessContext)
        {
            _logger.LogWarning("UserCache:{id} can't enter context", cachedUser.Id);
            context.Fail();
            return;
        }

        if (cachedUser.IsRootUser)
        {
            context.Succeed(requirement);
            return;
        }

        var descriptor = endpoint?.Metadata.GetMetadata<ControllerActionDescriptor>() ??
                         throw new ArgumentException("Can't find endpoint descriptor");

        var key = _center.GetKey(
            descriptor.ControllerTypeInfo.AsType(),
            descriptor.MethodInfo
        );

        var ok = await _roleCache.CheckPermissionByRoleIdsAsync(
            cachedUser.RoleIds,
            key,
            ct);

        if (!ok)
        {
            throw new IdentityErrNotPermitted(cachedUser.Id, key);
        }

        context.Succeed(requirement);
    }
}