using Alcedo.Core.Abstractions;

namespace Alcedo.Framework.FormulaEngine.FormulaCenter;

public class FormulaNotFoundException : AlcedoNotFoundException
{
    public FormulaNotFoundException(string message) : base(message)
    {
    }

    public FormulaNotFoundException(string message, string friendly) : base(message, friendly)
    {
    }
}