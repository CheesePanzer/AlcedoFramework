using Alcedo.Core.Domain.Kit.Test;
using Alcedo.Framework.Identity.Domain.Entity;

namespace Alcedo.Framework.Identity.Test.Domain;

public class DomainTestFixture
{
    public DomainTestFixture()
    {
        MetadataCenterTestExtensions.InitializeForTest(typeof(UserAgg).Assembly);
    }
}

[CollectionDefinition("Domain")]
public class DomainTestCollection : ICollectionFixture<DomainTestFixture>
{
}
