namespace OrderManagement.Application.DTOs.Products;

public class CreateProductRequest
{
    public string Name { get; set; } = null!;
    public string SKU { get; set; } = null!;
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public bool IsActive { get; set; }
}