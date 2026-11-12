using UUIDNext;

namespace Alcedo.Core.Domain.Entity;

public class EntityBase
{
    public Guid Id { get; protected set; } = Uuid.NewDatabaseFriendly(Database.PostgreSql);


    protected EntityBase()
    {
    }
}