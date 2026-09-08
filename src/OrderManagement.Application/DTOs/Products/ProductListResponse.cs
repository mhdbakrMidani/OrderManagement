namespace OrderManagement.Application.DTOs.Products;

public class ProductListResponse
{
    public List<ProductResponse> Items { get; set; } = [];
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}