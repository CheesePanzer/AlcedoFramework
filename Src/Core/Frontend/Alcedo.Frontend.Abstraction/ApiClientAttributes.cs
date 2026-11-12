namespace Alcedo.Frontend.Abstraction;

[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class)]
public class ApiClientAttribute : Attribute
{
    public string Controller { get; set; } = "";
    public string? Area { get; set; }
    public int Version { get; set; } = 1;
}

// --- Parameter Attributes ---

[AttributeUsage(AttributeTargets.Parameter)]
public sealed class ToRouteAttribute : Attribute;

[AttributeUsage(AttributeTargets.Parameter)]
public sealed class ToBodyAttribute : Attribute;

[AttributeUsage(AttributeTargets.Parameter)]
public sealed class ToQueryAttribute : Attribute;

[AttributeUsage(AttributeTargets.Parameter)]
public sealed class ToFormAttribute : Attribute { }

// --- HTTP Method Attributes ---

[AttributeUsage(AttributeTargets.Method)]
public sealed class HttpGetAttribute(string? template = null) : Attribute
{
    public string? Template { get; } = template;
}

[AttributeUsage(AttributeTargets.Method)]
public sealed class HttpPostAttribute(string? template = null) : Attribute
{
    public string? Template { get; } = template;
}

[AttributeUsage(AttributeTargets.Method)]
public sealed class HttpPutAttribute(string? template = null) : Attribute
{
    public string? Template { get; } = template;
}

[AttributeUsage(AttributeTargets.Method)]
public sealed class HttpPatchAttribute(string? template = null) : Attribute
{
    public string? Template { get; } = template;
}

[AttributeUsage(AttributeTargets.Method)]
public sealed class HttpDeleteAttribute(string? template = null) : Attribute
{
    public string? Template { get; } = template;
}