using OrderManagement.Application.DTOs.Products;
using OrderManagement.Domain.Entities;

namespace OrderManagement.Application.Mappings;

public static class ProductMapper
{
    public static ProductResponse ToResponse(Product product)
    {
        return new ProductResponse
        {
            Id = product.Id,
            Name = product.Name,
            SKU = product.SKU,
            Price = product.Price,
            StockQuantity = product.StockQuantity,
            IsActive = product.IsActive,
            CreatedAt = product.CreatedAt
        };
    }
}