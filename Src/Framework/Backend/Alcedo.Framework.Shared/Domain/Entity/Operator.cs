namespace Alcedo.Framework.Shared.Domain.Entity;

public sealed class OperatorRecord
{
    public OperatorRecord(Guid id, string code, string name)
    {
        Id = id;
        Code = code;
        Name = name;
    }

    public Guid Id { get; init; }

    public string Code { get; init; }

    public string Name { get; init; }

    public static OperatorRecord From(ContextOper oper)
    {
        return new OperatorRecord(oper.Id, oper.Code, oper.Name);
    }

    public OperatorRecord Clone()
    {
        return new OperatorRecord(Id, Code, Name);
    }
}