namespace Alcedo.Core.Abstractions;

public abstract class AppSettingsBase;

/// <summary>
/// Marks a property with its default value and description for documentation generation.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class AppSettingsDemoAttribute(string defaultValue, string description) : Attribute
{
    public string DefaultValue { get; } = defaultValue;
    public string Description { get; } = description;
}