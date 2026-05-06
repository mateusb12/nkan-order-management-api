using OrderManagement.Shared.Models;

namespace OrderManagement.Features.Orders.DTOs.Responses;

public class OrderDetailsResponse
{
    public int Id { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public OrderStatus Status { get; set; }

    public decimal TotalAmount { get; set; }

    public List<OrderItemResponse> Items { get; set; } = [];
}
