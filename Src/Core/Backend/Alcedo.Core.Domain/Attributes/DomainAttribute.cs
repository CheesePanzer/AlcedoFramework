namespace Alcedo.Core.Domain.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class AggregateRootDescriptionAttribute
    : Attribute
{
    public string AggCode { get; init; } //Unique In Whole Solution
    public string AggName { get; init; } //Human Friendly Name

    public AggregateRootDescriptionAttribute(string aggCode, string aggName)
    {
        AggCode = aggCode;
        AggName = aggName;
    }

    public AggregateRootDescriptionAttribute(string aggName)
    {
        AggName = aggName;
        AggCode = aggName;
    }
}