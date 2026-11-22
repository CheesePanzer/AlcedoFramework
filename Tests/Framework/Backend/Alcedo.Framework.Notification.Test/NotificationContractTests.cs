using System.Text.Json;
using Alcedo.Framework.Notification.Contracts;

namespace Alcedo.Framework.Notification.Test;

public class NotificationContractTests
{
    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("  ", false)]
    [InlineData("tester@example.com", true)]
    public void Recipient_EmailEligibilityRequiresNonBlankAddress(string? email, bool expected) =>
        Assert.Equal(expected, new RecipientContext("Tester", Guid.NewGuid(), email: email).CanEmail);

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("  ", false)]
    [InlineData("+8613812345678", true)]
    public void Recipient_PhoneEligibilityRequiresNonBlankNumber(string? phone, bool expected) =>
        Assert.Equal(expected, new RecipientContext("Tester", Guid.NewGuid(), phone: phone).CanPhone);

    [Theory]
    [InlineData(false, "User", false)]
    [InlineData(true, null, false)]
    [InlineData(true, " ", false)]
    [InlineData(true, "User", true)]
    public void Recipient_InAppRequiresBothIdentityAndType(bool hasId, string? type, bool expected) =>
        Assert.Equal(expected, new RecipientContext("Tester", hasId ? Guid.NewGuid() : Guid.Empty, type).CanInApp);

    public sealed class Payload { public string Name { get; set; } = ""; public List<Item> Items { get; set; } = []; }
    public sealed class Item { public decimal Amount { get; set; } }
    public sealed class Node { public Node? Child { get; set; } }

    [Fact]
    public void ExampleGenerator_CreatesValidJsonWithNestedCollection()
    {
        using var json = JsonDocument.Parse(MessageExampleGenerator.GenerateJsonExample(typeof(Payload)));
        Assert.Equal("string", json.RootElement.GetProperty("Name").GetString());
        var items = json.RootElement.GetProperty("Items");
        Assert.Equal(1, items.GetArrayLength());
        Assert.Equal(0m, items[0].GetProperty("Amount").GetDecimal());
    }

    [Fact]
    public void ExampleGenerator_RecursiveTypeTerminatesWithValidJson()
    {
        using var json = JsonDocument.Parse(MessageExampleGenerator.GenerateJsonExample(typeof(Node)));
        Assert.True(json.RootElement.TryGetProperty("Child", out _));
    }
}
