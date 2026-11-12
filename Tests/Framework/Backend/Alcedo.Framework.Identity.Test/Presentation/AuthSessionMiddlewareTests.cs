using System.Security.Claims;
using Alcedo.Core.Application;
using Alcedo.Core.Domain.DomException;
using Alcedo.Framework.Identity.Application.Dto.User;
using Alcedo.Framework.Identity.Domain.Entity;
using Alcedo.Framework.Identity.Exceptions;
using Alcedo.Framework.Identity.Contracts;
using Alcedo.Framework.Identity.Presentation;
using Alcedo.Framework.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;

namespace Alcedo.Framework.Identity.Test.Presentation;

public class AuthSessionMiddlewareTests
{
    private readonly Mock<IEntityCacheProvider<UserCacheDto>> _cacheMock = new();
    private readonly Mock<ILogger<AuthSessionMiddleware>> _loggerMock = new();
    private bool _nextCalled;
    private Task Next(HttpContext _) { _nextCalled = true; return Task.CompletedTask; }

    private HttpContext CreateContext(string? userId = null, string? sessionId = null, string? authScheme = null, bool isAuthenticated = true)
    {
        var context = new DefaultHttpContext();
        if (authScheme is not null || isAuthenticated)
        {
            var claims = new List<Claim>();
            if (userId is not null)
                claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));
            if (sessionId is not null)
                claims.Add(new Claim(AlcedoClaimNames.CurrentSession, sessionId));
            var identity = new ClaimsIdentity(claims, authScheme ?? AlcedoIdentityScheme.AuthenticationScheme);
            context.User = new ClaimsPrincipal(identity);
        }
        return context;
    }

    [Fact]
    public async Task AllowAnonymous_Endpoint_SkipsSessionCheck()
    {
        var middleware = new AuthSessionMiddleware(Next);
        var context = CreateContext();
        context.SetEndpoint(new Endpoint(_ => Task.CompletedTask,
            new EndpointMetadataCollection(new AllowAnonymousAttribute()), null));

        await middleware.InvokeAsync(context, _cacheMock.Object, _loggerMock.Object);

        Assert.True(_nextCalled);
    }

    [Fact]
    public async Task NonAlcedoAuthScheme_SkipsSessionCheck()
    {
        var middleware = new AuthSessionMiddleware(Next);
        var context = CreateContext(authScheme: "OtherScheme");
        context.SetEndpoint(new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(), null));

        await middleware.InvokeAsync(context, _cacheMock.Object, _loggerMock.Object);

        Assert.True(_nextCalled);
    }

    [Fact]
    public async Task ValidSession_PassesAndStoresCacheInItems()
    {
        var middleware = new AuthSessionMiddleware(Next);
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var context = CreateContext(userId.ToString(), sessionId.ToString());
        context.SetEndpoint(new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(), null));

        var cachedUser = new UserCacheDto
        {
            Id = userId, Code = "USR", Name = "Test", LoginName = "login",
            CurrentSession = sessionId, CanAccessContext = true, IsRootUser = false, RoleIds = []
        };
        _cacheMock.Setup(c => c.GetOrFallbackDbAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedUser);

        await middleware.InvokeAsync(context, _cacheMock.Object, _loggerMock.Object);

        Assert.True(_nextCalled);
        Assert.Same(cachedUser, context.Items[HttpContextUserStoreKey.Key]);
    }

    [Fact]
    public async Task SessionMismatch_Throws()
    {
        var middleware = new AuthSessionMiddleware(Next);
        var userId = Guid.NewGuid();
        var context = CreateContext(userId.ToString(), Guid.NewGuid().ToString());
        context.SetEndpoint(new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(), null));

        var cachedUser = new UserCacheDto
        {
            Id = userId, Code = "USR", Name = "Test", LoginName = "login",
            CurrentSession = Guid.NewGuid(), CanAccessContext = true, IsRootUser = false, RoleIds = []
        };
        _cacheMock.Setup(c => c.GetOrFallbackDbAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedUser);

        await Assert.ThrowsAsync<IdentityErrSessionInvalid>(() =>
            middleware.InvokeAsync(context, _cacheMock.Object, _loggerMock.Object));
    }

    [Fact]
    public async Task UserLocked_Throws()
    {
        var middleware = new AuthSessionMiddleware(Next);
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var context = CreateContext(userId.ToString(), sessionId.ToString());
        context.SetEndpoint(new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(), null));

        var cachedUser = new UserCacheDto
        {
            Id = userId, Code = "USR", Name = "Test", LoginName = "login",
            CurrentSession = sessionId, CanAccessContext = false, IsRootUser = false, RoleIds = []
        };
        _cacheMock.Setup(c => c.GetOrFallbackDbAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedUser);

        await Assert.ThrowsAsync<IdentityErrInvalidUser>(() =>
            middleware.InvokeAsync(context, _cacheMock.Object, _loggerMock.Object));
    }

    [Fact]
    public async Task UserNotFoundInDb_ThrowsUnauthenticated()
    {
        var middleware = new AuthSessionMiddleware(Next);
        var userId = Guid.NewGuid();
        var context = CreateContext(userId.ToString(), Guid.NewGuid().ToString());
        context.SetEndpoint(new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(), null));

        _cacheMock.Setup(c => c.GetOrFallbackDbAsync(userId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DomErrResourceNotFound<UserAgg>(userId));

        await Assert.ThrowsAsync<IdentityErrUnauthenticated>(() =>
            middleware.InvokeAsync(context, _cacheMock.Object, _loggerMock.Object));
    }
}
