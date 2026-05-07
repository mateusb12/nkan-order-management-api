using Microsoft.EntityFrameworkCore;
using OrderManagement.Features.Orders.DTOs;
using OrderManagement.Features.Orders.Services;
using OrderManagement.Shared.Data;
using OrderManagement.Shared.Exceptions;
using OrderManagement.Shared.Models;
using Xunit;

namespace OrderManagement.Tests.Features.Orders.Services;

public class OrderServiceTests
{
    private static Database CreateDatabase()
    {
        var options = new DbContextOptionsBuilder<Database>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new Database(options);
    }

    private static CreateOrderRequest CreateValidRequest()
    {
        return new CreateOrderRequest
        {
            CustomerName = "Maria Silva",
            Items =
            [
                new CreateOrderItemRequest
                {
                    ProductId = 1,
                    ProductName = "Notebook",
                    Quantity = 1,
                    UnitPrice = 3500m
                },
                new CreateOrderItemRequest
                {
                    ProductId = 2,
                    ProductName = "Mouse",
                    Quantity = 2,
                    UnitPrice = 80m
                }
            ]
        };
    }

    private static async Task<Order> SeedOrderAsync(
        Database db,
        string customerName = "Maria Silva",
        OrderStatus status = OrderStatus.Created,
        DateTime? createdAt = null)
    {
        var order = new Order
        {
            CustomerName = customerName,
            CreatedAt = createdAt ?? DateTime.UtcNow,
            Status = status,
            TotalAmount = 100m,
            Items =
            [
                new OrderItem
                {
                    ProductId = 1,
                    ProductName = "Notebook",
                    Quantity = 1,
                    UnitPrice = 100m
                }
            ]
        };

        db.Orders.Add(order);
        await db.SaveChangesAsync();

        return order;
    }

    [Fact]
    public async Task CriarPedidoAsync_DeveCriarPedidoComTotalCalculado()
    {
        await using var db = CreateDatabase();
        var service = new OrderService(db);
        var request = CreateValidRequest();
        
        var result = await service.CreateOrderAsync(request);
        
        Assert.NotEqual(0, result.Id);
        Assert.Equal("Maria Silva", result.CustomerName);
        Assert.Equal(OrderStatus.Created, result.Status);
        Assert.Equal(3660m, result.TotalAmount);
        Assert.Equal(2, result.Items.Count);
    }

    [Fact]
    public async Task CriarPedidoAsync_DevePersistirPedidoNoBanco()
    {
        await using var db = CreateDatabase();
        var service = new OrderService(db);
        var request = CreateValidRequest();

        var result = await service.CreateOrderAsync(request);

        var savedOrder = await db.Orders
            .Include(o => o.Items)
            .SingleAsync(o => o.Id == result.Id);

        Assert.Equal(result.Id, savedOrder.Id);
        Assert.Equal("Maria Silva", savedOrder.CustomerName);
        Assert.Equal(OrderStatus.Created, savedOrder.Status);
        Assert.Equal(3660m, savedOrder.TotalAmount);
        Assert.Equal(2, savedOrder.Items.Count);
    }

    [Fact]
    public async Task CriarPedidoAsync_DeveLancarErro_QuandoClienteEstiverVazio()
    {
        await using var db = CreateDatabase();
        var service = new OrderService(db);

        var request = CreateValidRequest();
        request.CustomerName = "";

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(request));

        Assert.Equal("Customer name is required.", exception.Message);
    }

    [Fact]
    public async Task CriarPedidoAsync_DeveLancarErro_QuandoPedidoNaoTiverItens()
    {
        await using var db = CreateDatabase();
        var service = new OrderService(db);

        var request = new CreateOrderRequest
        {
            CustomerName = "Maria Silva",
            Items = []
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(request));

        Assert.Equal("Order must contain at least one item.", exception.Message);
    }

    [Fact]
    public async Task CriarPedidoAsync_DeveLancarErro_QuandoQuantidadeDoItemForZero()
    {
        await using var db = CreateDatabase();
        var service = new OrderService(db);

        var request = CreateValidRequest();
        request.Items[0].Quantity = 0;

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(request));

        Assert.Equal("Item 'Notebook' must have quantity greater than zero.", exception.Message);
    }

    [Fact]
    public async Task CriarPedidoAsync_DeveLancarErro_QuandoPrecoUnitarioForZero()
    {
        await using var db = CreateDatabase();
        var service = new OrderService(db);

        var request = CreateValidRequest();
        request.Items[0].UnitPrice = 0m;

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(request));

        Assert.Equal("Item 'Notebook' must have unit price greater than zero.", exception.Message);
    }

    [Fact]
    public async Task CriarPedidoAsync_DeveLancarErro_QuandoNomeDoProdutoEstiverVazio()
    {
        await using var db = CreateDatabase();
        var service = new OrderService(db);

        var request = CreateValidRequest();
        request.Items[0].ProductName = "";

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateOrderAsync(request));

        Assert.Equal("Product name is required.", exception.Message);
    }

    [Fact]
    public async Task ListarPedidosAsync_DeveRetornarResumosPaginados()
    {
        await using var db = CreateDatabase();
        var service = new OrderService(db);

        await SeedOrderAsync(db, "First", createdAt: new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc));
        await SeedOrderAsync(db, "Second", createdAt: new DateTime(2026, 1, 2, 10, 0, 0, DateTimeKind.Utc));
        await SeedOrderAsync(db, "Third", createdAt: new DateTime(2026, 1, 3, 10, 0, 0, DateTimeKind.Utc));

        var firstPage = await service.GetOrdersAsync(page: 1, pageSize: 2);
        var secondPage = await service.GetOrdersAsync(page: 2, pageSize: 2);

        Assert.Equal(2, firstPage.Count);
        Assert.Equal("Third", firstPage[0].CustomerName);
        Assert.Equal("Second", firstPage[1].CustomerName);

        Assert.Single(secondPage);
        Assert.Equal("First", secondPage[0].CustomerName);
    }

    [Theory]
    [InlineData(0, 10, "Page must be greater than zero.")]
    [InlineData(-1, 10, "Page must be greater than zero.")]
    [InlineData(1, 0, "Page size must be greater than zero.")]
    [InlineData(1, -10, "Page size must be greater than zero.")]
    public async Task ListarPedidosAsync_DeveLancarErro_QuandoPaginacaoForInvalida(
        int page,
        int pageSize,
        string expectedMessage)
    {
        await using var db = CreateDatabase();
        var service = new OrderService(db);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GetOrdersAsync(page, pageSize));

        Assert.Equal(expectedMessage, exception.Message);
    }

    [Fact]
    public async Task BuscarPedidoPorIdAsync_DeveRetornarPedidoComItens()
    {
        await using var db = CreateDatabase();
        var service = new OrderService(db);

        var order = await SeedOrderAsync(db);

        var result = await service.GetOrderByIdAsync(order.Id);

        Assert.Equal(order.Id, result.Id);
        Assert.Equal("Maria Silva", result.CustomerName);
        Assert.Single(result.Items);
        Assert.Equal("Notebook", result.Items[0].ProductName);
    }

    [Fact]
    public async Task BuscarPedidoPorIdAsync_DeveLancarErro_QuandoPedidoNaoExistir()
    {
        await using var db = CreateDatabase();
        var service = new OrderService(db);

        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => service.GetOrderByIdAsync(999));

        Assert.Equal("Order 999 not found.", exception.Message);
    }

    [Fact]
    public async Task CancelarPedidoAsync_DeveCancelarPedido()
    {
        await using var db = CreateDatabase();
        var service = new OrderService(db);

        var order = await SeedOrderAsync(db);

        var result = await service.CancelOrderAsync(order.Id);

        Assert.Equal(OrderStatus.Cancelled, result.Status);

        var savedOrder = await db.Orders.FindAsync(order.Id);
        Assert.NotNull(savedOrder);
        Assert.Equal(OrderStatus.Cancelled, savedOrder.Status);
    }

    [Fact]
    public async Task CancelarPedidoAsync_DeveLancarErro_QuandoPedidoJaEstiverCancelado()
    {
        await using var db = CreateDatabase();
        var service = new OrderService(db);

        var order = await SeedOrderAsync(db, status: OrderStatus.Cancelled);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CancelOrderAsync(order.Id));

        Assert.Equal($"Order {order.Id} is already cancelled.", exception.Message);
    }

    [Fact]
    public async Task CancelarPedidoAsync_DeveLancarErro_QuandoPedidoNaoExistir()
    {
        await using var db = CreateDatabase();
        var service = new OrderService(db);

        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => service.CancelOrderAsync(999));

        Assert.Equal("Order 999 not found.", exception.Message);
    }
}
