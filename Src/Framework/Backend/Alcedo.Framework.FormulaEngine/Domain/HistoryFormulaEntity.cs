using Alcedo.Core.Domain.Entity;

namespace Alcedo.Framework.FormulaEngine.Domain;

public sealed class HistoryFormulaEntity : EntityBase
{
    public string Code { get; private set; }
    
    public string Description { get; private set; }
    
    public int OriginalVersion { get; private set; } 
    
    public List<HistoryFormulaInputEntity> InputParameters {get; private set;} = new();
    
    public List<HistoryFormulaExpressionEntity> Expressions{get; private set;} = new();
    
    public List<HistoryFormulaOutputEntity> OutputParameters {get; private set;} = new();

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    private HistoryFormulaEntity()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    {
        
    }

    public HistoryFormulaEntity(FormulaAggRoot formula)
    {
        Code = formula.Code;
        Description = formula.Description;
        OriginalVersion = formula.Version;
    
        InputParameters = formula.InputParameters.Select(p => new HistoryFormulaInputEntity(
            Id, p.VarName, p.VarDescription, p.VarType, p.ValidCondition, p.DefaultValue)).ToList();
    
        Expressions = formula.Expressions.Select(e => new HistoryFormulaExpressionEntity(
            Id, e.Order, e.VarName, e.Expression, e.Note)).ToList();
    
        OutputParameters = formula.OutputParameters.Select(p => new HistoryFormulaOutputEntity(
            Id, p.VarName, p.VarDescription, p.VarType, p.Expression)).ToList();
    }
}

public sealed class HistoryFormulaInputEntity : EntityBase
{
    public Guid FormulaId { get; private set; }
    
    public string VarName { get; private set; }
    
    public string? VarDescription { get; private set; }
    
    public string VarType { get; private set; }
    
    public string? ValidCondition { get; private set; }
    
    public string? DefaultValue { get; private set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    private HistoryFormulaInputEntity()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    {
        
    }

    public HistoryFormulaInputEntity(Guid formulaId, string varName, string? varDescription, string varType, string? validCondition, string? defaultValue)
    {
        FormulaId = formulaId;
        VarName = varName;
        VarDescription = varDescription;
        VarType = varType;
        ValidCondition = validCondition;
        DefaultValue = defaultValue;
    }
}

public sealed class HistoryFormulaExpressionEntity : EntityBase
{
    public Guid FormulaId { get; private set; }
    
    public int Order { get; private set; }
    
    public string VarName { get; private set; }
    
    public string Expression { get; private set; }
    
    public string? Note { get; private set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    private HistoryFormulaExpressionEntity()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    {
        
    }

    public HistoryFormulaExpressionEntity(Guid formulaId, int order, string varName, string expression, string? note)
    {
        FormulaId = formulaId;
        Order = order;
        VarName = varName;
        Expression = expression;
        Note = note;
    }
}

public sealed class HistoryFormulaOutputEntity : EntityBase
{
    public Guid FormulaId { get; private set; }
    
    public string VarName { get; private set; }
    
    public string? VarDescription { get; private set; }
    
    public string VarType { get; private set; }
    
    public string Expression { get; private set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    private HistoryFormulaOutputEntity()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    {
        
    }
    
    public HistoryFormulaOutputEntity(Guid formulaId, string varName, string? varDescription, string varType, string expression)
    {
        FormulaId = formulaId;
        VarName = varName;
        VarDescription = varDescription;
        VarType = varType;
        Expression = expression;
    }
}