using OrderManagement.Application.DTOs.Orders;
using OrderManagement.Domain.Entities;

namespace OrderManagement.Application.Mappings;

public static class OrderMapper
{
    public static OrderResponse ToResponse(Order order)
    {
        return new OrderResponse
        {
            Id = order.Id,
            CustomerId = order.CustomerId,
            OrderDate = order.OrderDate,
            Status = order.Status,
            TotalAmount = order.TotalAmount,
            CreatedAt = order.CreatedAt,
            Items = order.Items.Select(item => new OrderItemResponse
            {
                Id = item.Id,
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                TotalPrice = item.TotalPrice
            }).ToList()
        };
    }
}