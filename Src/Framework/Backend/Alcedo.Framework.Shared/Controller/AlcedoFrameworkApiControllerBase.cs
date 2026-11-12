using System.Security.Claims;
using Alcedo.Core.Presentation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Alcedo.Framework.Shared.Controller;

public static class FClaimsPrincipalExtensions
{
    public static ContextOper ToFContextUser(this ClaimsPrincipal user) => new()
    {
        Id = Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var guid) ? guid : Guid.Empty,
        Name = user.Identity?.Name ?? "Anonymous",
        IsAuthenticated = user.Identity?.IsAuthenticated ?? false,
        Code = user.FindFirstValue(AlcedoClaimNames.UserCode) ?? "Anonymous"
    };
}

[ApiController]
[Authorize(Policy = AlcedoAuthorizePolicy.Rbac)]
[Route("api/v{version:apiVersion}/[controller]")]
public abstract class AlcedoFmApiControllerBase : AlcedoApiControllerBase
{
    /// <summary>
    /// Don't access this, use CurrentUser
    /// </summary>
    private ContextOper? _currentUser;

    protected new ContextOper CurrentUser => _currentUser ??= User.ToFContextUser();
}

[ApiController]
[Authorize(Policy = AlcedoAuthorizePolicy.Rbac)]
[Area("F")]
[Route("api/v{version:apiVersion}/[area]/[controller]")]
public abstract class AlcedoFmApiAreaControllerBase : AlcedoApiAreaControllerBase
{
    private ContextOper? _currentUser;

    protected new ContextOper CurrentUser => _currentUser ??= User.ToFContextUser();
}
