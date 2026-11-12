using Alcedo.Core.Domain.Entity;
using Alcedo.Framework.Shared;
using Alcedo.Framework.Shared.Domain.Entity;

namespace Alcedo.Framework.FormulaEngine.Domain;

public sealed class FormulaAggRoot : ReferenceDataAggregateRootBase
{
    public string Description { get; private set; }
    
    private List<FormulaInputEntity> _inputParameters = new();
    
    public IReadOnlyList<FormulaInputEntity> InputParameters => _inputParameters;
    
    private List<FormulaExpressionEntity> _expressions = new();
    
    public IReadOnlyList<FormulaExpressionEntity> Expressions => _expressions;
    
    private List<FormulaOutputEntity> _outputParameters = new();
    
    public IReadOnlyList<FormulaOutputEntity> OutputParameters => _outputParameters;
    
    public OperatorRecord Updater { get; private set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    private FormulaAggRoot()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    {
        
    }

    public FormulaAggRoot(
        string code, 
        string description,
        IReadOnlyList<ParameterCreateCommand> inputParameters,
        IReadOnlyList<ParameterCreateCommand> outputParameters) : base(code)
    {
        Description = description;
        Updater = new OperatorRecord(SystemUser.Id, SystemUser.Code, SystemUser.Name);
        InitializeInputParameters(inputParameters);
        InitializeOutputParameters(outputParameters);
    }

    private void InitializeInputParameters(IReadOnlyList<ParameterCreateCommand> parameters)
    {
        if (_inputParameters.Count != 0)
        {
            throw new InvalidOperationException("Cannot initialize input parameters since the aggroot is not empty");
        }
        
        foreach (var parameter in parameters)
        {
            _inputParameters.Add(new FormulaInputEntity(Id, parameter.VarName, parameter.VarDescription, parameter.VarType));
        }
    }

    private void InitializeOutputParameters(IReadOnlyList<ParameterCreateCommand> parameters)
    {
        if (_outputParameters.Count != 0)
        {
            throw new InvalidOperationException("Cannot initialize output parameters since the aggroot is not empty");
        }

        foreach (var parameter in parameters)
        {
            _outputParameters.Add(new FormulaOutputEntity(Id, parameter.VarName, parameter.VarDescription, parameter.VarType));
        }
    }
    
    public void CreateOrUpdateExpressions(IReadOnlyCollection<(int Order, string VarName, string Expression, string? Note)> expressions, OperatorRecord updater)
    {
        _expressions.Clear();
        
        foreach (var expression in expressions)
        {
            var exprEnt = new FormulaExpressionEntity(Id, expression.Order, expression.VarName, expression.Expression, expression.Note);
            
            _expressions.Add(exprEnt);
        }
        
        Updater = updater;
    }

    public void UpdateInputParams(
        IReadOnlyCollection<(Guid FormulaInputId, string? ValidCondition, string? DefaultValue)> inputParameters, OperatorRecord updater)
    {
        foreach (var inputParam in inputParameters)
        {
            var toBeUpdated = _inputParameters.Single(p => p.Id == inputParam.FormulaInputId);
            
            toBeUpdated.UpdateExpression(inputParam.ValidCondition, inputParam.DefaultValue);
        }
        
        Updater = updater;
    }

    public void UpdateOutputParams(IReadOnlyCollection<(Guid FormulaInputId, string Expression)> outputParameters, OperatorRecord updater)
    {
        foreach (var outputParam in outputParameters)
        {
            var toBeUpdated = _outputParameters.Single(p => p.Id == outputParam.FormulaInputId);
            
            toBeUpdated.UpdateExpression(outputParam.Expression);
        }
        
        Updater = updater;
    }
}

public sealed record ParameterCreateCommand
{
    public ParameterCreateCommand(string varName,  string varType, string? varDescription)
    {
        VarName = varName;
        VarDescription = varDescription;
        VarType = varType;
    }

    public string VarName { get; init; }
    
    public string? VarDescription { get; init; }
    
    public string VarType { get; init; }
}

public sealed class FormulaInputEntity : EntityBase
{
    public Guid FormulaId { get; private set; }
    
    public string VarName { get; private set; }
    
    public string? VarDescription { get; private set; }
    
    public string VarType { get; private set; }
    
    public string? ValidCondition { get; private set; }
    
    public string? DefaultValue { get; private set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    private FormulaInputEntity()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    {
        
    }

    public FormulaInputEntity(Guid formulaId, string varName, string? varDescription, string varType)
    {
        FormulaId = formulaId;
        VarName = varName;
        VarDescription = varDescription;
        VarType = varType;
    }
    
    public void UpdateExpression(string? validCondition, string? defaultValue)
    {
        ValidCondition = validCondition;
        
        DefaultValue = defaultValue;
    }
}

public sealed class FormulaExpressionEntity : EntityBase
{
    public Guid FormulaId { get; private set; }
    
    public int Order { get; private set; }
    
    public string VarName { get; private set; }
    
    public string Expression { get; private set; }
    
    public string? Note { get; private set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    private FormulaExpressionEntity()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    {
        
    }

    public FormulaExpressionEntity(Guid formulaId, int order, string varName, string expression, string? note)
    {
        FormulaId = formulaId;
        Order = order;
        VarName = varName;
        Expression = expression;
        Note = note;
    }
}

public sealed class FormulaOutputEntity : EntityBase
{
    public Guid FormulaId { get; private set; }
    
    public string VarName { get; private set; }
    
    public string? VarDescription { get; private set; }
    
    public string VarType { get; private set; }
    
    public string Expression { get; private set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    private FormulaOutputEntity()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    {
        
    }
    
    public FormulaOutputEntity(Guid formulaId, string varName, string? varDescription, string varType)
    {
        FormulaId = formulaId;
        VarName = varName;
        VarDescription = varDescription;
        VarType = varType;
        Expression = string.Empty;
    }

    public void UpdateExpression(string expression)
    {
        Expression = expression;
    }
}