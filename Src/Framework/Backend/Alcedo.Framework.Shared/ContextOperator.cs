using Alcedo.Core.Abstractions;
using Alcedo.Framework.Shared.Domain.Entity;

namespace Alcedo.Framework.Shared;

public sealed record ContextOper : ContextUser
{
    public string Code { get; set; } = "Anonymous";

    public string SnapShot => $"[{Code}] {Name}";

    public OperatorRecord ToRecord()
    {
        return new OperatorRecord(Id, Code, Name);
    }
}