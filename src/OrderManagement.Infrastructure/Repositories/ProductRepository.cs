using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.DTOs.Products;
using OrderManagement.Application.Interfaces.Repositories;
using OrderManagement.Domain.Entities;
using OrderManagement.Infrastructure.Persistence;

namespace OrderManagement.Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly ApplicationDbContext _context;

    public ProductRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Product?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);
    }

    public async Task<List<Product>> GetByIdsAsync(
        IEnumerable<int> ids,
        CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .Where(x => ids.Contains(x.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<ProductListResponse> GetListAsync(
        ProductListRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Products
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();

            query = query.Where(x =>
                x.Name.Contains(search) ||
                x.SKU.Contains(search));
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(x =>
                x.IsActive == request.IsActive.Value);
        }

        query = request.SortBy?.ToLower() switch
        {
            "price" => request.SortDescending
                ? query.OrderByDescending(x => x.Price)
                : query.OrderBy(x => x.Price),

            "name" => request.SortDescending
                ? query.OrderByDescending(x => x.Name)
                : query.OrderBy(x => x.Name),

            _ => query.OrderBy(x => x.Id)
        };

        var totalCount = await query.CountAsync(
            cancellationToken);

        var pageNumber = request.PageNumber < 1
            ? 1
            : request.PageNumber;

        var pageSize = request.PageSize < 1
            ? 10
            : Math.Min(request.PageSize, 100);

        var products = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var totalPages = (int)Math.Ceiling(
            totalCount / (double)pageSize);

        return new ProductListResponse
        {
            Items = products
                .Select(x => new ProductResponse
                {
                    Id = x.Id,
                    Name = x.Name,
                    SKU = x.SKU,
                    Price = x.Price,
                    StockQuantity = x.StockQuantity,
                    IsActive = x.IsActive,
                    CreatedAt = x.CreatedAt
                })
                .ToList(),

            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };
    }

    public async Task<bool> ExistsAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await _context.Products
            .AnyAsync(
                x => x.Id == id,
                cancellationToken);
    }

    public async Task AddAsync(
        Product product,
        CancellationToken cancellationToken = default)
    {
        await _context.Products.AddAsync(
            product,
            cancellationToken);
    }
}