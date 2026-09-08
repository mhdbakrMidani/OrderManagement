namespace OrderManagement.Application.DTOs.Orders;

public class CreateOrderItemRequest
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}