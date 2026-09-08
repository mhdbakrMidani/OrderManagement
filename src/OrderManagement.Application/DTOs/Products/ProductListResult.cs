using OrderManagement.Domain.Entities;

namespace OrderManagement.Application.DTOs.Products;

public class ProductListResult
{
    public List<Product> Items { get; set; } = [];
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}