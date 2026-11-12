namespace Alcedo.Framework.FormulaEngine.Contract.Abstraction;

[AttributeUsage(AttributeTargets.Class)]
public sealed class FormulaAttribute : Attribute
{
    public FormulaAttribute(string? code = null, string? name  = null, string? description  = null)
    {
        Description = description;
        Name = name;
        Code = code;
    }

    public string? Code { get; init; }
    
    public string? Name { get; init; }
    
    public string? Description { get; init; }
}

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class ParamAttribute : Attribute
{
    public string? VariableName { get; init; }
    
    public string? Description { get; init; }
    
    public ParamAttribute(string? variableName = null,string? description = null)
    {
        VariableName = variableName;
        
        Description = description;
    }
}