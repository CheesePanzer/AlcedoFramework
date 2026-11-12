namespace Alcedo.Framework.FormulaEngine.Contract.Abstraction;

public abstract class Formula<TInput, TOutput>
where TInput : class
where TOutput : class, new()
{
    public Type InputType => typeof(TInput);
    
    public Type OutputType => typeof(TOutput);
}