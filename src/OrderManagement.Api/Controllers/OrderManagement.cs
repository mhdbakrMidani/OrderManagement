using Microsoft.AspNetCore.Mvc;
using OrderManagement.Application.DTOs.Orders;
using OrderManagement.Application.Services.Orders;

namespace OrderManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly OrderService _orderService;

    public OrdersController(OrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpPost]
    public async Task<ActionResult<OrderResponse>> Create(
        CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        var order = await _orderService.CreateAsync(
            request,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = order.Id },
            order);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderResponse>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var order = await _orderService.GetByIdAsync(
            id,
            cancellationToken);

        return Ok(order);
    }
}