using OrderManagement.Shared.Models;

namespace OrderManagement.Features.Orders.Events;

public sealed record OrderCreatedEvent(
    int OrderId,
    string CustomerName,
    DateTime CreatedAt,
    string Status,
    decimal TotalAmount,
    List<OrderCreatedItemEvent> Items)
{
    public static OrderCreatedEvent From(Order order)
    {
        return new OrderCreatedEvent(
            OrderId: order.Id,
            CustomerName: order.CustomerName,
            CreatedAt: order.CreatedAt,
            Status: order.Status.ToString(),
            TotalAmount: order.TotalAmount,
            Items: order.Items.Select(item => new OrderCreatedItemEvent(
                ProductId: item.ProductId,
                ProductName: item.ProductName,
                Quantity: item.Quantity,
                UnitPrice: item.UnitPrice
            )).ToList());
    }
}

public sealed record OrderCreatedItemEvent(
    int ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice);