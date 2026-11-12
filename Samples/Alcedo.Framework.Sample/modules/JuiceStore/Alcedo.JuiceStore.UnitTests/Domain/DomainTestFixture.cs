using Alcedo.Core.Domain.Kit.Test;
using Alcedo.JuiceStore.Backend.Domain;

namespace Alcedo.JuiceStore.UnitTests.Domain;

public sealed class DomainTestFixture
{
    public DomainTestFixture()
    {
        MetadataCenterTestExtensions.InitializeForTest(typeof(JuiceAggRoot).Assembly);
    }
}

[CollectionDefinition("JuiceStoreDomain")]
public sealed class DomainTestCollection : ICollectionFixture<DomainTestFixture>;
