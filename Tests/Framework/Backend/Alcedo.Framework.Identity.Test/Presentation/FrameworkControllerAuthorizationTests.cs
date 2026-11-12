using Alcedo.Framework.Admin;
using Alcedo.Framework.FormulaEngine;
using Alcedo.Framework.Identity.Contracts;
using Alcedo.Framework.Notification;
using Alcedo.Framework.Shared;
using Alcedo.Framework.Shared.Controller;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Alcedo.Framework.Identity.Test.Presentation;

public class FrameworkControllerAuthorizationTests
{
    private static readonly Type[] ModuleMarkers =
    [
        typeof(AlcedoAdminModule),
        typeof(AlcedoFormulaEngineModule),
        typeof(AlcedoIdentityModule),
        typeof(AlcedoNotificationModule)
    ];

    [Fact]
    public void FrameworkApiBases_RequireRbacPolicy()
    {
        AssertRbacPolicy(typeof(AlcedoFmApiControllerBase));
        AssertRbacPolicy(typeof(AlcedoFmApiAreaControllerBase));
    }

    [Fact]
    public void FrameworkControllers_InheritFrameworkApiBase()
    {
        var invalidControllers = ModuleMarkers
            .Select(t => t.Assembly)
            .Distinct()
            .SelectMany(a => a.GetExportedTypes())
            .Where(t => !t.IsAbstract && typeof(ControllerBase).IsAssignableFrom(t))
            .Where(t => !typeof(AlcedoFmApiControllerBase).IsAssignableFrom(t) &&
                        !typeof(AlcedoFmApiAreaControllerBase).IsAssignableFrom(t))
            .Select(t => t.FullName)
            .ToArray();

        Assert.True(invalidControllers.Length == 0,
            $"Framework controllers must inherit a secured Framework API base: {string.Join(", ", invalidControllers)}");
    }

    private static void AssertRbacPolicy(Type controllerType)
    {
        var authorize = controllerType
            .GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>()
            .SingleOrDefault(a => a.Policy == AlcedoAuthorizePolicy.Rbac);

        Assert.NotNull(authorize);
    }
}
