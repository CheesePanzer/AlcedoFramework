namespace Alcedo.Framework.Notification.Contracts;

/*
 * Initialize all members in class or the Scriban may throw exceptions
 * eg.
[MessageType("TMsgOrderPaid", description: "Test order paid")]
public record OrderPaymentNotificationPayload : INotificationMessage
{
    public string SystemName { get; init; } = string.Empty;
    public UserInfo User { get; init; } = new();
    public OrderInfo Order { get; init; } = new();
}

public record UserInfo
{
    public string Name { get; init; } = string.Empty;
}

public record OrderInfo
{
    public string OrderNo { get; init; } = string.Empty;
    public decimal PaymentAmount { get; init; }
    public string PaymentMethod { get; init; } = string.Empty;
    public DateTime PaidAt { get; init; }
    public List<OrderItemInfo> Items { get; init; } = new();
}

public record OrderItemInfo
{
    public string ProductName { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
}
 */

/// <summary>
/// Mark a class as a notification payload
/// </summary>
/// <remarks>
/// Remember to Initialize all members in class or the Scriban may throw exceptions
/// </remarks>
public interface INotificationMessage;