using OrderManagement.Application.DTOs.Products;
using OrderManagement.Application.Interfaces;
using OrderManagement.Application.Interfaces.Repositories;
using OrderManagement.Application.Mappings;
using OrderManagement.Domain.Entities;

namespace OrderManagement.Application.Services.Products;

public class ProductService
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ProductService(
        IProductRepository productRepository,
        IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ProductResponse> CreateAsync(
        CreateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        var product = new Product
        {
            Name = request.Name,
            SKU = request.SKU,
            Price = request.Price,
            StockQuantity = request.StockQuantity,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        await _productRepository.AddAsync(
            product,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return ProductMapper.ToResponse(product);
    }

    public async Task<ProductResponse?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdAsync(
            id,
            cancellationToken);

        if (product is null)
        {
            return null;
        }

        return ProductMapper.ToResponse(product);
    }

    public async Task<ProductListResponse> GetListAsync(
        ProductListRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _productRepository.GetListAsync(
            request,
            cancellationToken);

        return new ProductListResponse
        {
            Items = result.Items
                .Select(ProductMapper.ToResponse)
                .ToList(),

            PageNumber = result.PageNumber,
            PageSize = result.PageSize,
            TotalCount = result.TotalCount,
            TotalPages = result.TotalPages
        };
    }
}