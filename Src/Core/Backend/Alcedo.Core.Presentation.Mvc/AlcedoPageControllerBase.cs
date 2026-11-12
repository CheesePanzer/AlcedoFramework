using Alcedo.Core.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Alcedo.Core.Presentation.Mvc;

[Route("[controller]")]
public abstract class AlcedoPageControllerBase : Controller
{
    private ContextUser? _currentUser;

    /// <summary>
    /// Use this instead of original user to get the user in current request
    /// </summary>
    protected ContextUser CurrentUser => _currentUser ??= User.ToContextUser();
}

[Area("Alcedo")]
[Route("[area]/[controller]")]
public abstract class AlcedoPageAreaControllerBase : Controller
{
    private ContextUser? _currentUser;

    /// <summary>
    /// Use this instead of original user to get the user in current request
    /// </summary>
    protected ContextUser CurrentUser => _currentUser ??= User.ToContextUser();
}