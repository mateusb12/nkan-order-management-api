using Microsoft.AspNetCore.Mvc;
using OrderManagement.Features.Orders.DTOs;
using OrderManagement.Features.Orders.Services;
using OrderManagement.Shared.Exceptions;

namespace OrderManagement.Features.Orders.Controllers;

[ApiController]
[Route("orders")]
public class OrdersController(OrderService orders) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetOrders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await orders.GetOrdersAsync(page, pageSize);

        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetOrderById(int id)
    {
        try
        {
            var order = await orders.GetOrderByIdAsync(id);

            return Ok(order);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new
            {
                error = ex.Message
            });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder(CreateOrderRequest request)
    {
        var order = await orders.CreateOrderAsync(request);

        return CreatedAtAction(
            nameof(GetOrderById),
            new { id = order.Id },
            order);
    }

    [HttpPut("{id:int}/cancel")]
    public async Task<IActionResult> CancelOrder(int id)
    {
        try
        {
            var order = await orders.CancelOrderAsync(id);

            return Ok(order);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new
            {
                error = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                error = ex.Message
            });
        }
    }
}
