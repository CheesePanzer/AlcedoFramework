using Alcedo.Core.Domain.Entity;

namespace Alcedo.Framework.Shared.Domain.Entity;

public abstract class ReferenceDataAggregateRootBase : AggregateRoot
{
    public string Code { get; protected set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    protected ReferenceDataAggregateRootBase()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    {
    }

    protected ReferenceDataAggregateRootBase(
        string code)
    {
        Code = code;
    }
}