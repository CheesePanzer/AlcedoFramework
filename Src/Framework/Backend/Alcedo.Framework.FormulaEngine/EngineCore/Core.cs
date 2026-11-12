using System.Reflection;
using Alcedo.Framework.FormulaEngine.Contract;
using Alcedo.Framework.FormulaEngine.Contract.Abstraction;
using Humanizer;
using Microsoft.Extensions.Logging;
using NCalc;
using Serilog;

namespace Alcedo.Framework.FormulaEngine.EngineCore;

internal static class EngineInputFlatter
{
    public static async Task<Dictionary<string, object?>> FlattenAndValidateAsync<TInput>(
        TInput input,
        IReadOnlyCollection<InputParameter> inputEntities)
        where TInput : class
    {
        var parameters = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        CollectValues(input, parameters,  "In_");

        foreach (var entity in inputEntities)
        {
            if (!parameters.TryGetValue(entity.VarName, out var value))
            {
                throw new InvalidOperationException(
                    $"Input variable '{entity.VarName}' not found in flattened input. TInput may be out of sync.");
            }

            if (entity.ValidCondition is not null)
            {
                var condExpr = new AsyncExpression(entity.ValidCondition, ExpressionOptions.AllowNullParameter);
                foreach (var (k, v) in parameters)
                {
                    condExpr.Parameters[k] = v!;
                }

                condExpr.Parameters["self"] = value!;

                var result = await condExpr.EvaluateAsync();
                if (result is not bool isValid || !isValid)
                {
                    throw new FormulaValidationException(
                        $"Variable '{entity.VarName}' failed condition: {entity.ValidCondition}");
                }
            }

            if (value is null && entity.DefaultValue is not null)
            {
                parameters[entity.VarName] = Convert.ChangeType(entity.DefaultValue, ResolveType(entity.VarType));
            }
        }

        return parameters;
    }

    private static Type ResolveType(string varType) => varType.ToLowerInvariant() switch
    {
        "decimal" or "decimal?" => typeof(decimal),
        "int" or "int?" => typeof(int),
        "string" or "string?" => typeof(string),
        "bool" or "bool?" => typeof(bool),
        "datetime" or "datetime?" => typeof(DateTime),
        _ => throw new InvalidOperationException($"Unsupported variable type: {varType}")
    };

    private static void CollectValues<TInput>(TInput input, Dictionary<string, object?> parameters, string prefix)
        where TInput : class
    {
        foreach (var prop in input.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var attr = prop.GetCustomAttribute<ParamAttribute>();
            var segmentName = attr?.VariableName ?? prop.Name;
            var fullName = prefix + segmentName;
            var propType = prop.PropertyType;
            var value = prop.GetValue(input);

            if (FormulaParamFlattener.IsPrimitive(propType))
            {
                parameters[fullName] = value;
            }
            else if (attr?.VariableName is not null)
            {
                throw new InvalidOperationException(
                    $"Property '{prop.Name}' is a complex type but has a VariableName override '{attr.VariableName}'.");
            }
            else
            {
                if (value is null)
                {
                    FillNullLeaves(propType, parameters, fullName + "_"); //when a object is null, then the nodes under it are all null
                }
                else
                {
                    CollectValues(value, parameters, fullName + "_");
                }
            }
        }
    }
    
    private static void FillNullLeaves(Type type, Dictionary<string, object?> parameters, string prefix)
    {
        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var attr = prop.GetCustomAttribute<ParamAttribute>();
            var segmentName = attr?.VariableName ?? prop.Name;
            var fullName = prefix + segmentName;

            if (FormulaParamFlattener.IsPrimitive(prop.PropertyType))
            {
                parameters[fullName] = null;
            }
            else
            {
                FillNullLeaves(prop.PropertyType, parameters, fullName + "_");
            }
        }
    }
}

public sealed class EngineCore<TInput, TOutput>
where TInput : class
{
    private readonly ILogger<EngineCore<TInput, TOutput>> _logger;
    
    private readonly FormulaExecutionContext _context;
    
    private readonly TInput _inputInstance;

    private readonly IReadOnlyCollection<InputParameter> _inputParameters;

    private readonly IReadOnlyCollection<ExpressionParameter> _expressionsSorted;

    private readonly IReadOnlyCollection<OutputParameter> _outputParameters;

    private Dictionary<string, object?> _inputs = new Dictionary<string, object?>();
    
    private Dictionary<string, object?> _outputs = new Dictionary<string, object?>();
    
    public EngineCore(TInput inputInstance, string formulaCode, int formulaVersion, IReadOnlyCollection<InputParameter> inputParameters, IReadOnlyCollection<ExpressionParameter> expressions,  IReadOnlyCollection<OutputParameter> outputParameters, ILogger<EngineCore<TInput, TOutput>> logger)
    {
        _inputInstance = inputInstance;
        _inputParameters = inputParameters;
        _context = FormulaExecutionContext.Start(formulaCode, formulaVersion, inputInstance);
        _outputParameters = outputParameters;
        _logger = logger;
        _expressionsSorted = expressions.OrderBy(e => e.Order).ToList();
    }

    private async Task OnInitializeAsync()
    {
        _inputs = await EngineInputFlatter.FlattenAndValidateAsync(_inputInstance, _inputParameters);
    }

    private async Task InternalParseAsync(CancellationToken cancellationToken = default)
    {
        var context = new Dictionary<string, object?>();
        
        var intermediateSteps = new List<object>();

        foreach (var input in _inputs)
        {
            context.Add(input.Key, input.Value);
        }

        foreach (var formula in _expressionsSorted)
        {
            var expr = new AsyncExpression(formula.Expression, ExpressionOptions.AllowNullParameter);

            foreach (var obj in context)
            {
                expr.Parameters.Add(obj.Key, obj.Value);
            }
            
            var result = await expr.EvaluateAsync(cancellationToken);

            if (!context.TryAdd(formula.VarName, result))
            {
                throw new FormulaExecutionException($"Duplicate middle variable name {formula.VarName} for formula {_context.FormulaCode}");
            }
            
            intermediateSteps.Add(new { VarName = formula.VarName, Order = formula.Order, Value = result });
        }
        
        _context.SetIntermediateSteps(intermediateSteps);
        
        foreach (var outputParam in _outputParameters)
        {
            var expr = new AsyncExpression(outputParam.Expression, ExpressionOptions.AllowNullParameter);
            
            foreach (var obj in context)
            {
                expr.Parameters.Add(obj.Key, obj.Value);
            }
            
            var result  = await expr.EvaluateAsync(cancellationToken);
            
            if (!_outputs.TryAdd(outputParam.VarName, result))
            {
                throw new FormulaExecutionException($"Duplicate output variable name {outputParam.VarName} for formula {_context.FormulaCode}");
            }
        }
    }

    private TOutput ConstructOutput()
    {
        var output = Activator.CreateInstance<TOutput>();

        foreach (var (varName, value) in _outputs)
        {
            SetNestedValue(output!, varName, value);
        }

        return output;
    }

    public async Task<EngineResult<TOutput>> ParseAsync(CancellationToken cancellationToken = default)
    {
        await OnInitializeAsync();
        
        await InternalParseAsync(cancellationToken);

        var output = ConstructOutput();
        
        _context.Finish(output);
        
        _logger.LogInformation(
            "Formula executed. TaskId={TaskId} Code={Code} Version={Version} DurationMs={DurationMs} Input={Input} Output={Output}",
            _context.TaskId, _context.FormulaCode, _context.FormulaVersion, _context.DurationMs,
            _context.InputJson, _context.OutputJson);
        
        return new EngineResult<TOutput>(output,_context.TaskId, _context.FormulaCode, _context.FormulaVersion);
    }

    private static void SetNestedValue(object target, string varName, object? value)
    {
        var segments = varName.Split('_', 2);

        if (segments.Length == 1)
        {
            var prop = target.GetType().GetProperty(varName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
                       ?? throw new InvalidOperationException(
                           $"Property '{varName}' not found on {target.GetType().Name}.");

            var converted = value is null
                ? null
                : Convert.ChangeType(value, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType);

            prop.SetValue(target, converted);
        }
        else
        {
            var prop = target.GetType().GetProperty(segments[0])
                       ?? throw new InvalidOperationException(
                           $"Property '{segments[0]}' not found on {target.GetType().Name}.");

            var nested = prop.GetValue(target);

            if (nested is null)
            {
                nested = Activator.CreateInstance(prop.PropertyType)
                         ?? throw new InvalidOperationException(
                             $"Cannot create instance of '{prop.PropertyType.Name}'.");
                prop.SetValue(target, nested);
            }

            SetNestedValue(nested, segments[1], value);
        }
    }
}