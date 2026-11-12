using System.Security.Claims;
using Alcedo.Core.Application;
using Alcedo.Core.Presentation;
using Alcedo.Framework.Identity.Application.Dto.User;
using Alcedo.Framework.Identity.Application.Provider;
using Alcedo.Framework.Identity.Contracts;
using Alcedo.Framework.Identity.Presentation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;

namespace Alcedo.Framework.Identity.Test.Presentation;

public class AlcedoRbacHandlerTests
{
    private readonly Mock<IEntityCacheProvider<UserCacheDto>> _userCacheMock = new();
    private readonly Mock<IRoleCacheProvider> _roleCacheMock = new();
    private readonly EndpointResourceOperationCenter _center = new([]);
    private readonly Mock<ILogger<AlcedoRbacHandler>> _loggerMock = new();
    private readonly AlcedoRbacHandler _handler;

    public AlcedoRbacHandlerTests()
    {
        _handler = new AlcedoRbacHandler(_userCacheMock.Object, _roleCacheMock.Object, _center, _loggerMock.Object);
    }

    private static AuthorizationHandlerContext CreateContext(HttpContext httpContext, ClaimsPrincipal? user = null)
    {
        var req = new AlcedoPermissionRequirement();
        return new AuthorizationHandlerContext([req], user ?? new ClaimsPrincipal(), httpContext);
    }

    private static DefaultHttpContext CreateHttpContext(string endpointKey = null)
    {
        return new DefaultHttpContext();
    }

    [Fact]
    public async Task AllowAnonymous_Endpoint_Succeeds()
    {
        var httpContext = CreateHttpContext();
        httpContext.SetEndpoint(new Endpoint(_ => Task.CompletedTask,
            new EndpointMetadataCollection(new AllowAnonymousAttribute()), null));
        var context = CreateContext(httpContext);

        await _handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task RootUser_AccessesAnyEndpoint_Succeeds()
    {
        var httpContext = CreateHttpContext();
        httpContext.SetEndpoint(new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(), null));

        var userId = Guid.NewGuid();
        var cachedUser = new UserCacheDto
        {
            Id = userId, Code = "root", Name = "Root", LoginName = "root",
            CanAccessContext = true, IsRootUser = true, RoleIds = [], CurrentSession = Guid.NewGuid()
        };
        httpContext.Items[HttpContextUserStoreKey.Key] = cachedUser;

        var principal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        ], "TestAuth"));
        var context = CreateContext(httpContext, principal);

        await _handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task UserNotInHttpContextCache_FallsBackToProvider()
    {
        var userId = Guid.NewGuid();
        var httpContext = CreateHttpContext();
        httpContext.SetEndpoint(new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(), null));

        var principal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        ], "TestAuth"));
        var context = CreateContext(httpContext, principal);

        var cachedUser = new UserCacheDto
        {
            Id = userId, Code = "USR", Name = "Test", LoginName = "login",
            CanAccessContext = false, IsRootUser = false, RoleIds = [], CurrentSession = Guid.NewGuid()
        };
        _userCacheMock.Setup(c => c.GetOrFallbackDbAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedUser);

        await _handler.HandleAsync(context);

        Assert.True(context.HasFailed);
        _userCacheMock.Verify(
            c => c.GetOrFallbackDbAsync(userId, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
