using Alcedo.Core.Abstractions;

namespace Alcedo.Framework.FormulaEngine.EngineCore;

public sealed class FormulaValidationException : Exception
{
    public FormulaValidationException(string message) : base(message) { }
}

public sealed class FormulaExecutionException : AlcedoBadRequestException
{
    public FormulaExecutionException(string message) : base(message)
    {
    }

    public FormulaExecutionException(string message, string friendly) : base(message, friendly)
    {
    }
}