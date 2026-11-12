namespace Alcedo.Core.Presentation;

[AttributeUsage(AttributeTargets.Class)]
public class ResourceAttribute : Attribute
{
    public string Code { get; }

    public string Description { get; }

    public ResourceAttribute(string code, string? description = null)
    {
        Code = code;
        Description = description ?? code;
    }
}

[AttributeUsage(AttributeTargets.Method)]
public class OperationAttribute : Attribute
{
    public string Code { get; }

    public string Description { get; }

    public OperationAttribute(string code, string? description = null)
    {
        Code = code;
        Description = description ?? code;
    }
}