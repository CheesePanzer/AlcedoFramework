using System.Reflection;
using Microsoft.AspNetCore.Mvc;

namespace Alcedo.Core.Presentation.Test;

[Resource("TEST", "TestResource")]
public class TestPermissionController : ControllerBase
{
    [HttpGet]
    [Operation("READ", "ReadAction")]
    public IActionResult Read() => Ok();

    [HttpPost]
    [Operation("CREATE", "CreateAction")]
    public IActionResult Create() => Ok();

    [HttpGet]
    [NonAction]
    [Operation("IGNORED", "IgnoredAction")]
    public IActionResult Ignored() => Ok();
}

public class ControllerWithoutResource : ControllerBase
{
    [HttpGet]
    [Operation("READ", "ReadAction")]
    public IActionResult Read() => Ok();
}

[Resource("NOOP", "NoOpResource")]
public class ControllerWithMissingOperation : ControllerBase
{
    [HttpGet]
    public IActionResult Read() => Ok();
}

[Area("Admin")]
[Resource("ADMIN", "AdminResource")]
public class AreaPermissionController : ControllerBase
{
    [HttpGet]
    [Operation("LIST", "ListAction")]
    public IActionResult List() => Ok();
}

public class EndpointPermissionCenterTests
{
    private static Assembly FakeAssembly(params Type[] types) => new TestAssembly(types);

    [Fact]
    public void GetKey_WithoutArea_FormatsCorrectly()
    {
        var center = new EndpointResourceOperationCenter([FakeAssembly(typeof(TestPermissionController))]);
        var controller = typeof(TestPermissionController);
        var method = controller.GetMethod(nameof(TestPermissionController.Read))!;

        var key = center.GetKey(controller, method);

        Assert.Equal("test.read", key);
    }

    [Fact]
    public void GetKey_WithArea_IncludesAreaPrefix()
    {
        var center = new EndpointResourceOperationCenter([FakeAssembly(typeof(AreaPermissionController))]);
        var controller = typeof(AreaPermissionController);
        var method = controller.GetMethod(nameof(AreaPermissionController.List))!;

        var key = center.GetKey(controller, method);

        Assert.Equal("admin.admin.list", key);
    }

    [Fact]
    public void GetDescription_ReturnsResourceAndActionDescriptions()
    {
        var center = new EndpointResourceOperationCenter([FakeAssembly(typeof(TestPermissionController))]);
        var controller = typeof(TestPermissionController);
        var method = controller.GetMethod(nameof(TestPermissionController.Read))!;

        var (resourceDesc, actionDesc) = center.GetDescription(controller, method);

        Assert.Equal("TestResource", resourceDesc);
        Assert.Equal("ReadAction", actionDesc);
    }

    [Fact]
    public void GetKey_ThrowsWhenNotInitialized()
    {
        var center = new EndpointResourceOperationCenter([FakeAssembly(typeof(TestPermissionController))]);
        var unknownController = typeof(ControllerWithoutResource);
        var unknownMethod = unknownController.GetMethod(nameof(ControllerWithoutResource.Read))!;

        Assert.Throws<ArgumentException>(() => center.GetKey(unknownController, unknownMethod));
    }

    [Fact]
    public void Constructor_UsesConventionWhenControllerMissingResourceAttribute()
    {
        var center = new EndpointResourceOperationCenter([FakeAssembly(typeof(ControllerWithoutResource))]);
        var controller = typeof(ControllerWithoutResource);
        var method = controller.GetMethod(nameof(ControllerWithoutResource.Read))!;

        Assert.Equal("controller-without-resource.read", center.GetKey(controller, method));
    }

    [Fact]
    public void Constructor_UsesConventionWhenActionMissingOperationAttribute()
    {
        var center = new EndpointResourceOperationCenter([FakeAssembly(typeof(ControllerWithMissingOperation))]);
        var controller = typeof(ControllerWithMissingOperation);
        var method = controller.GetMethod(nameof(ControllerWithMissingOperation.Read))!;

        Assert.Equal("noop.read", center.GetKey(controller, method));
        Assert.Equal(("NoOpResource", "Read"), center.GetDescription(controller, method));
    }

    [Fact]
    public void Constructor_SkipsNonActionMethods()
    {
        var center = new EndpointResourceOperationCenter([FakeAssembly(typeof(TestPermissionController))]);
        var controller = typeof(TestPermissionController);
        var ignoredMethod = controller.GetMethod(nameof(TestPermissionController.Ignored))!;

        Assert.Throws<ArgumentException>(() => center.GetKey(controller, ignoredMethod));
    }

    [Fact]
    public void Constructor_SkipsMethodsWithoutHttpMethodAttribute()
    {
        var center = new EndpointResourceOperationCenter([FakeAssembly(typeof(TestPermissionController))]);
        var controller = typeof(TestPermissionController);

        var methods = controller.GetMethods()
            .Where(m => !m.IsDefined(typeof(NonActionAttribute), false))
            .ToList();

        // Only READ and CREATE have HttpGet/HttpPost; any plain method without HTTP attr is skipped
        var plainMethod = controller.GetMethod("ToString");
        Assert.NotNull(plainMethod);
        Assert.Throws<ArgumentException>(() => center.GetKey(controller, plainMethod));
    }

    private class TestAssembly : Assembly
    {
        private readonly Type[] _types;
        public TestAssembly(params Type[] types) => _types = types;
        public override Type[] GetExportedTypes() => _types;
        public override string FullName => "TestAssembly";
    }
}
