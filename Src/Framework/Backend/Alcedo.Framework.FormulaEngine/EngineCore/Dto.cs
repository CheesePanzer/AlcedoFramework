using System.Text.Json;

namespace Alcedo.Framework.FormulaEngine.EngineCore;

public sealed class FormulaExecutionContext
{
    public Guid TaskId { get; } = Guid.NewGuid();
    public DateTimeOffset StartedAt { get; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? FinishedAt { get; private set; }
    public long? DurationMs { get; private set; }
    public required string FormulaCode { get; init; }
    public required int FormulaVersion { get; init; }
    public string InputJson { get; private set; } = string.Empty;
    public string? IntermediateJson { get; private set; }
    public string? OutputJson { get; private set; }

    public static FormulaExecutionContext Start<TInput>(string code, int version, TInput input) where TInput : class
    {
        return new()
        {
            FormulaCode = code,
            FormulaVersion = version,
            InputJson = JsonSerializer.Serialize(input),
        };
    }
    
    public void SetIntermediateSteps(List<object> steps)
    {
        IntermediateJson = JsonSerializer.Serialize(steps);
    }

    public void Finish<TOutput>(TOutput output)
    {
        FinishedAt = DateTimeOffset.UtcNow;
        DurationMs = (long)(FinishedAt.Value - StartedAt).TotalMilliseconds;
        OutputJson = JsonSerializer.Serialize(output);
    }
}

public record InputParameter
{
    public required string VarName { get; init; }
    
    public required string VarType { get; init; }
    
    public string? ValidCondition { get; init; }
    
    public string? DefaultValue { get; init; }
}

public record ExpressionParameter
{
    public required int Order { get; init; }
    
    public required string VarName { get; init; }
    
    public required string Expression { get; init; }
}

public record OutputParameter
{
    public required string VarName { get; init; }
    
    public string? VarDescription { get; init; }
    
    public required string VarType { get; init; }
    
    public required string Expression { get; init; }

}