using Alcedo.Framework.FormulaEngine.Contract.Abstraction;

namespace Alcedo.Framework.FormulaEngine.Contract;

public sealed record EngineResult<TOutput>
{
    public EngineResult(TOutput output, Guid taskId, string formulaCode, int formulaVersion)
    {
        Output = output;
        TaskId = taskId;
        FormulaCode = formulaCode;
        FormulaVersion = formulaVersion;
    }

    public TOutput Output { get; init; }
    public Guid TaskId { get; init; }
    public string FormulaCode { get; init; }
    public int FormulaVersion { get; init; }
}

public interface IFormulaEngine
{
    Task<EngineResult<TOutput>> ExecuteAsync<TFormula, TInput, TOutput>(TInput input, CancellationToken ct)
        where TFormula : Formula<TInput, TOutput> where TInput : class where TOutput : class, new();
    
    Task<EngineResult<TOutput>> ExecuteAsync<TFormula, TInput, TOutput>(TInput input, int version, CancellationToken ct)
        where TFormula : Formula<TInput, TOutput> where TInput : class where TOutput : class, new();
}