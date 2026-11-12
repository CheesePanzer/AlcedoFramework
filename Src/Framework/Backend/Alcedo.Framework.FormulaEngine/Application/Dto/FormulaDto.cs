using Alcedo.Core.Application.Dto;
using Alcedo.Framework.Shared.Application.Dto;

namespace Alcedo.Framework.FormulaEngine.Application.Dto;

public sealed record FormulaDto : ReferenceDataDtoBase
{
    public required string Description { get; init; }
    
    public required List<FormulaInputDto> InputParameters { get; init; }
    
    public required List<FormulaExpressionDto> Expressions { get; init; }
    
    public required List<FormulaOutputDto> OutputParameters { get; init; }
}

public sealed record FormulaInputDto : EntityDtoBase
{
    public required Guid FormulaId { get; init; }
    
    public required string VarName { get; init; }
    
    public required string? VarDescription { get; init; }
    
    public required string VarType { get; init; }
    
    public required string? ValidCondition { get; init; }
    
    public required string? DefaultValue { get; init; }
}

public sealed record FormulaExpressionDto : EntityDtoBase
{
    public required Guid FormulaId { get; init; }
    
    public required int Order { get; init; }
    
    public required string VarName { get; init; }
    
    public required string Expression { get; init; }
    
    public required string? Note { get; init; }
}

public sealed record FormulaOutputDto : EntityDtoBase
{
    public required Guid FormulaId { get; init; }
    
    public required string VarName { get; init; }
    
    public required string? VarDescription { get; init; }
    
    public required string VarType { get; init; }
    
    public required string Expression { get; init; }
}