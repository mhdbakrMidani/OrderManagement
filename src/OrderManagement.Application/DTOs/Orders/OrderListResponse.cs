namespace OrderManagement.Application.DTOs.Orders;

public class OrderListResponse
{
    public List<OrderResponse> Items { get; set; } = [];
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}