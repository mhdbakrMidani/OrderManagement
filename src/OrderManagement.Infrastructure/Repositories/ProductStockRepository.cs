using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Interfaces.Repositories;
using OrderManagement.Infrastructure.Persistence;

namespace OrderManagement.Infrastructure.Repositories;

public class ProductStockRepository : IProductStockRepository
{
    private readonly ApplicationDbContext _context;

    public ProductStockRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> DecreaseStockAsync(
        int productId,
        int quantity,
        CancellationToken cancellationToken = default)
    {
        var affectedRows = await _context.Products
            .Where(x =>
                x.Id == productId &&
                x.IsActive &&
                x.StockQuantity >= quantity)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(
                        x => x.StockQuantity,
                        x => x.StockQuantity - quantity),
                cancellationToken);

        return affectedRows == 1;
    }

    public async Task IncreaseStockAsync(
        int productId,
        int quantity,
        CancellationToken cancellationToken = default)
    {
        await _context.Products
            .Where(x => x.Id == productId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(
                        x => x.StockQuantity,
                        x => x.StockQuantity + quantity),
                cancellationToken);
    }
}