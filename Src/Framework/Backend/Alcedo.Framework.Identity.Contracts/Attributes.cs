namespace Alcedo.Framework.Identity.Contracts;

public interface ISkipPermissionCheck;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class SkipPermissionCheckAttribute
    : Attribute, ISkipPermissionCheck;