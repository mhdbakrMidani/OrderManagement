using OrderManagement.Application.DTOs.Products;
using OrderManagement.Domain.Entities;

namespace OrderManagement.Application.Interfaces.Repositories;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<List<Product>> GetByIdsAsync(
        IEnumerable<int> ids,
        CancellationToken cancellationToken = default);

    Task<ProductListResponse> GetListAsync(
        ProductListRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Product product,
        CancellationToken cancellationToken = default);
}