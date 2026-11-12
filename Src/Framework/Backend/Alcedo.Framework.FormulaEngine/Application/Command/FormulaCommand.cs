using System.ComponentModel.DataAnnotations;
using Alcedo.Core.Presentation.Validation;
using Alcedo.Framework.Shared.Application.Command;

namespace Alcedo.Framework.FormulaEngine.Application.Command;

public sealed record FormulaExpressionUpdateCommand : IValidatableObject
{
    public required int FormulaVersion { get; init; }
    
    public required IReadOnlyCollection<FormulaExpressionUpdateCommandItem> Items { get; init; }
    
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        HashSet<string> varNames = new HashSet<string>();

        foreach (var item in Items)
        {
            if (!varNames.Add(item.VarName))
            {
                yield return new ValidationResult($"Duplicate variable name: {item.VarName}", new[] { nameof(Items) });
            }
        }
    }
}

public sealed record FormulaExpressionUpdateCommandItem
{
    public required int Order { get; init; }
    
    public required string VarName { get; init; }
    [TextValue]
    public required string Expression { get; init; }
    [TextValue]
    public string? Note { get; init; }
}

public sealed record FormulaInputUpdateCommand
{
    public required int FormulaVersion { get; init; }

    public required IReadOnlyList<FormulaInputUpdateCommandItem> Items { get; init; }
}

public sealed record FormulaInputUpdateCommandItem
{
    public required Guid InputParamId { get; init; }
    [TextValue]
    public string? ValidCondition { get; init; }
    
    public string? DefaultValue { get; init; }
}

public sealed record FormulaOutputUpdateCommand
{
    public required int FormulaVersion { get; init; }

    public required IReadOnlyList<FormulaOutputUpdateCommandItem> Items { get; init; }
}

public sealed record FormulaOutputUpdateCommandItem
{
    public required Guid OutputParamId { get; init; }
    [TextValue]
    public required string Expression { get; init; }
}