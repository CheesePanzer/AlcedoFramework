using Alcedo.Framework.Shared.Domain.Entity;

namespace Alcedo.Framework.Shared;

public static class SystemUser
{
    public static readonly Guid Id = Guid.Parse("00000000-0000-0000-0000-000000000001");
    public static readonly string Code = "System";
    public static readonly string Name = "System";

    public static OperatorRecord ToRecord()
    {
        return new OperatorRecord(Id, Code, Name);
    }
}