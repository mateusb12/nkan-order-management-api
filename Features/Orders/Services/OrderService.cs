using Microsoft.EntityFrameworkCore;
using OrderManagement.Features.Orders.DTOs;
using OrderManagement.Features.Orders.DTOs.Responses;
using OrderManagement.Shared.Data;
using OrderManagement.Shared.Exceptions;
using OrderManagement.Shared.Models;

namespace OrderManagement.Features.Orders.Services;

public class OrderService(Database db)
{
    public async Task<OrderDetailsResponse> CreateOrderAsync(CreateOrderRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CustomerName))
        {
            throw new InvalidOperationException("Customer name is required.");
        }

        if (request.Items.Count == 0)
        {
            throw new InvalidOperationException("Order must contain at least one item.");
        }

        var orderItems = request.Items.Select(item =>
        {
            if (item.Quantity <= 0)
            {
                throw new InvalidOperationException(
                    $"Item '{item.ProductName}' must have quantity greater than zero.");
            }

            if (item.UnitPrice <= 0)
            {
                throw new InvalidOperationException(
                    $"Item '{item.ProductName}' must have unit price greater than zero.");
            }

            return new OrderItem
            {
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice
            };
        }).ToList();

        var totalAmount = orderItems.Sum(item =>
            item.Quantity * item.UnitPrice);

        var order = new Order
        {
            CustomerName = request.CustomerName,
            CreatedAt = DateTime.UtcNow,
            Status = OrderStatus.Created,
            TotalAmount = totalAmount,
            Items = orderItems
        };

        db.Orders.Add(order);

        await db.SaveChangesAsync();

        return new OrderDetailsResponse
        {
            Id = order.Id,
            CustomerName = order.CustomerName,
            CreatedAt = order.CreatedAt,
            Status = order.Status,
            TotalAmount = order.TotalAmount,
            Items = order.Items.Select(item => new OrderItemResponse
            {
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice
            }).ToList()
        };
    }

    public async Task<List<OrderSummaryResponse>> GetOrdersAsync(
        int page,
        int pageSize)
    {
        if (page <= 0)
        {
            throw new InvalidOperationException("Page must be greater than zero.");
        }

        if (pageSize <= 0)
        {
            throw new InvalidOperationException("Page size must be greater than zero.");
        }

        return await db.Orders
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new OrderSummaryResponse
            {
                Id = o.Id,
                CustomerName = o.CustomerName,
                CreatedAt = o.CreatedAt,
                Status = o.Status,
                TotalAmount = o.TotalAmount
            })
            .ToListAsync();
    }

    public async Task<OrderDetailsResponse> GetOrderByIdAsync(int id)
    {
        var order = await db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order is null)
        {
            throw new NotFoundException($"Order {id} not found.");
        }

        return new OrderDetailsResponse
        {
            Id = order.Id,
            CustomerName = order.CustomerName,
            CreatedAt = order.CreatedAt,
            Status = order.Status,
            TotalAmount = order.TotalAmount,
            Items = order.Items.Select(item => new OrderItemResponse
            {
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice
            }).ToList()
        };
    }

    public async Task<OrderDetailsResponse> CancelOrderAsync(int id)
    {
        var order = await db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order is null)
        {
            throw new NotFoundException($"Order {id} not found.");
        }

        if (order.Status == OrderStatus.Cancelled)
        {
            throw new InvalidOperationException(
                $"Order {id} is already cancelled.");
        }

        order.Status = OrderStatus.Cancelled;

        await db.SaveChangesAsync();

        return new OrderDetailsResponse
        {
            Id = order.Id,
            CustomerName = order.CustomerName,
            CreatedAt = order.CreatedAt,
            Status = order.Status,
            TotalAmount = order.TotalAmount,
            Items = order.Items.Select(item => new OrderItemResponse
            {
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice
            }).ToList()
        };
    }
}
