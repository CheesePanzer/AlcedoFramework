using System.Security.Claims;
using Alcedo.Core.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Alcedo.Core.Presentation;

public static class ClaimsPrincipalExtensions
{
    public static ContextUser ToContextUser(this ClaimsPrincipal user) => new()
    {
        Id = Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var guid) ? guid : Guid.Empty,
        Name = user.Identity?.Name ?? "Anonymous",
        IsAuthenticated = user.Identity?.IsAuthenticated ?? false
    };
}

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public abstract class AlcedoApiControllerBase : ControllerBase
{
    private ContextUser? _currentUser;

    /// <summary>
    /// Use this instead of original user to get the user in current request
    /// </summary>
    protected ContextUser CurrentUser => _currentUser ??= User.ToContextUser();
}

[ApiController]
[Area("Alcedo")]
[Route("api/v{version:apiVersion}/[area]/[controller]")]
public abstract class AlcedoApiAreaControllerBase : ControllerBase
{
    private ContextUser? _currentUser;

    /// <summary>
    /// Use this instead of original user to get the user in current request
    /// </summary>
    protected ContextUser CurrentUser => _currentUser ??= User.ToContextUser();
}