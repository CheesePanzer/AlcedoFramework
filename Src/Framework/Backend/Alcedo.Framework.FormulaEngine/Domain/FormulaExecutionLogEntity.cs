using Alcedo.Core.Domain.Entity;

namespace Alcedo.Framework.FormulaEngine.Domain;

public sealed class FormulaExecutionLogEntity : EntityBase
{
    public string FomulaCode { get; private set; }
    
    public string FormulaVersion { get; private set; }
    
    public string InputJson { get; private set; }
    
    public string OutputJson { get; private set; }
    
    public DateTimeOffset ExecutedAt { get; private set; }
    
    public long DurationMs { get; private set; }

    public FormulaExecutionLogEntity(
        string fomulaCode, 
        string formulaVersion, 
        string inputJson, 
        string outputJson,
        DateTimeOffset executedAt,
        long durationMs)
    {
        FomulaCode = fomulaCode;
        FormulaVersion = formulaVersion;
        InputJson = inputJson;
        OutputJson = outputJson;
        ExecutedAt = executedAt;
        DurationMs = durationMs;
    }
}