using Alcedo.Core.Domain.DomException;
using Alcedo.Core.Domain.Kit.Test;
using Alcedo.Framework.Shared.Domain.Entity;
using Alcedo.JuiceStore.Backend.Domain;

namespace Alcedo.JuiceStore.UnitTests.Domain;

[Collection("JuiceStoreDomain")]
public sealed class JuiceAggRootTests
{
    private static OperatorRecord CreateOperator(string code = "tester", string name = "Test Operator") =>
        new(Guid.NewGuid(), code, name);

    private static JuiceAggRoot CreateJuice(
        string code = "ORANGE-250",
        string name = "Orange Spark",
        string description = "Fresh orange juice with a bright citrus finish.",
        string flavor = "Orange",
        int volumeMl = 250,
        decimal price = 4.50m,
        string? imageUrl = "https://example.test/orange.jpg",
        bool isFeatured = true,
        int sortOrder = 10,
        OperatorRecord? creator = null) =>
        new(code, name, description, flavor, volumeMl, price, imageUrl, isFeatured, sortOrder,
            creator ?? CreateOperator());

    [Fact]
    public void Constructor_SetsPropertiesAndDraftStatus()
    {
        var juice = CreateJuice();

        Assert.Equal("ORANGE-250", juice.Code);
        Assert.Equal("Orange Spark", juice.Name);
        Assert.Equal("Fresh orange juice with a bright citrus finish.", juice.Description);
        Assert.Equal("Orange", juice.Flavor);
        Assert.Equal(250, juice.VolumeMl);
        Assert.Equal(4.50m, juice.Price);
        Assert.Equal("https://example.test/orange.jpg", juice.ImageUrl);
        Assert.True(juice.IsFeatured);
        Assert.Equal(10, juice.SortOrder);
        Assert.Equal(MasterDataStates.Draft, juice.Status);
    }

    [Fact]
    public void Constructor_CreatesAuditEvent()
    {
        var juice = CreateJuice();

        var events = juice.CollectEventsAndClearForTest();

        Assert.Contains(events, e => e is MasterDataAuditLogEvent);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_NonPositiveVolume_Throws(int volumeMl)
    {
        Assert.Throws<DomErrRuleViolation>(() => CreateJuice(volumeMl: volumeMl));
    }

    [Fact]
    public void Constructor_NegativePrice_Throws()
    {
        Assert.Throws<DomErrRuleViolation>(() => CreateJuice(price: -0.01m));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_BlankDescription_Throws(string description)
    {
        Assert.Throws<DomErrRuleViolation>(() => CreateJuice(description: description));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_BlankFlavor_Throws(string flavor)
    {
        Assert.Throws<DomErrRuleViolation>(() => CreateJuice(flavor: flavor));
    }

    [Fact]
    public void Update_ModifiesJuiceFieldsAndUpdater()
    {
        var juice = CreateJuice();
        juice.ClearEventsForTest();
        var updater = CreateOperator("editor", "Catalog Editor");

        juice.Update("Berry Lift", "Mixed berry juice.", "Berry", 330, 5.75m,
            "https://example.test/berry.jpg", false, 20, updater);

        Assert.Equal("Berry Lift", juice.Name);
        Assert.Equal("Mixed berry juice.", juice.Description);
        Assert.Equal("Berry", juice.Flavor);
        Assert.Equal(330, juice.VolumeMl);
        Assert.Equal(5.75m, juice.Price);
        Assert.Equal("https://example.test/berry.jpg", juice.ImageUrl);
        Assert.False(juice.IsFeatured);
        Assert.Equal(20, juice.SortOrder);
        Assert.Equal(updater.Id, juice.Updater.Id);
        Assert.Contains(juice.CollectEventsAndClearForTest(), e => e is MasterDataAuditLogEvent);
    }

    [Fact]
    public void EnableAndDisable_FollowMasterDataStateFlow()
    {
        var juice = CreateJuice();
        var oper = CreateOperator();

        juice.Enable(oper);
        Assert.Equal(MasterDataStates.Enabled, juice.Status);

        juice.Disable(oper);
        Assert.Equal(MasterDataStates.Disabled, juice.Status);
    }

    [Fact]
    public void Delete_EnabledJuice_Throws()
    {
        var juice = CreateJuice();
        var oper = CreateOperator();
        juice.Enable(oper);

        Assert.Throws<DomErrStatusActionInvalid>(() => juice.Delete(oper));
    }

    [Fact]
    public void Delete_DisabledJuice_CreatesAuditEvent()
    {
        var juice = CreateJuice();
        var oper = CreateOperator();
        juice.Enable(oper);
        juice.Disable(oper);
        juice.ClearEventsForTest();

        juice.Delete(oper);

        Assert.Contains(juice.CollectEventsAndClearForTest(), e => e is MasterDataAuditLogEvent);
    }
}
