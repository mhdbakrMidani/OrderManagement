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

    [HttpGet]
    public async Task<ActionResult<OrderListResponse>> GetList(
        [FromQuery] OrderListRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _orderService.GetListAsync(
            request,
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("{id:int}/confirm")]
    public async Task<IActionResult> Confirm(
        int id,
        CancellationToken cancellationToken)
    {
        await _orderService.ConfirmAsync(
            id,
            cancellationToken);

        return NoContent();
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(
        int id,
        CancellationToken cancellationToken)
    {
        await _orderService.CancelAsync(
            id,
            cancellationToken);

        return NoContent();
    }
}