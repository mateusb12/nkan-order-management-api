using Microsoft.AspNetCore.Mvc;

namespace OrderManagement.Features.Orders.Controllers;

[ApiController]
[Route("orders")]
public class OrdersController : ControllerBase
{
    [HttpGet]
    public IActionResult GetOrders()
    {
        return Ok(new
        {
            Message = "Hello from OrdersController"
        });
    }
}
