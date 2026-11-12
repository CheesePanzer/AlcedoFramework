using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;

namespace Alcedo.Framework.FormulaEngine.EngineCore;

public static class EngineSandboxRunner
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    public static async Task<string> RunSandboxAsync(
        string inputJsonStr,
        Type tInputType,
        Type tOutputType,
        string formulaCode,
        int formulaVersion,
        IReadOnlyCollection<InputParameter> inputParameters,
        IReadOnlyCollection<ExpressionParameter> expressions,
        IReadOnlyCollection<OutputParameter> outputParameters,
        CancellationToken cancellationToken = default)
    {
        object? inputInstance = JsonSerializer.Deserialize(inputJsonStr, tInputType, JsonOptions);
        if (inputInstance == null)
        {
            throw new ArgumentException($"Cannot deserialize {tInputType.Name}");
        }

        Type openEngineType = typeof(EngineCore<,>);
        Type[] typeArgs = { tInputType, tOutputType };
        Type constructedEngineType = openEngineType.MakeGenericType(typeArgs);
        
        Type openLoggerType = typeof(NullLogger<>);
        Type constructedLoggerType = openLoggerType.MakeGenericType(constructedEngineType);

        object loggerInstance = constructedLoggerType
            .GetField("Instance", BindingFlags.Public | BindingFlags.Static)!
            .GetValue(null)!;

        object[] constructorArgs = {
            inputInstance,
            formulaCode,
            formulaVersion,
            inputParameters,
            expressions,
            outputParameters,
            loggerInstance // 传入 NullLogger
        };

        object engineInstance = Activator.CreateInstance(constructedEngineType, constructorArgs)
                                ?? throw new InvalidOperationException($"Can not create instance of {constructedEngineType.Name}");

        var parseMethod = constructedEngineType.GetMethod(nameof(EngineCore<object, object>.ParseAsync))
                          ?? throw new InvalidOperationException("cannot find ParseAsync method");

        var task = (Task)parseMethod.Invoke(engineInstance, new object[] { cancellationToken })!;
        await task.ConfigureAwait(false);

        var resultProperty = task.GetType().GetProperty(nameof(Task<object>.Result))
                             ?? throw new InvalidOperationException("Can not get result property");
        object engineResult = resultProperty.GetValue(task)!;

        var outputProperty = engineResult.GetType().GetProperty("Output") 
                             ?? throw new InvalidOperationException("Can not find output property in EngineResult");
        object? outputInstance = outputProperty.GetValue(engineResult);

        return JsonSerializer.Serialize(outputInstance, JsonOptions);
    }
}