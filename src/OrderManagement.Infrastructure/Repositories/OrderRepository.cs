using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Interfaces.Repositories;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.Enums;
using OrderManagement.Infrastructure.Persistence;

namespace OrderManagement.Infrastructure.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly ApplicationDbContext _context;

    public OrderRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Order?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .Include(x => x.Items)
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);
    }

    public async Task AddAsync(
        Order order,
        CancellationToken cancellationToken = default)
    {
        await _context.Orders.AddAsync(
            order,
            cancellationToken);
    }

    public async Task<bool> TryConfirmAsync(
    int id,
    CancellationToken cancellationToken = default)
    {
        var affectedRows = await _context.Orders
            .Where(x =>
                x.Id == id &&
                x.Status == OrderStatus.Pending)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(
                        x => x.Status,
                        OrderStatus.Confirmed),
                cancellationToken);

        return affectedRows == 1;
    }

    public async Task<bool> TryCancelAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var affectedRows = await _context.Orders
            .Where(x =>
                x.Id == id &&
                x.Status == OrderStatus.Pending)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(
                        x => x.Status,
                        OrderStatus.Cancelled),
                cancellationToken);

        return affectedRows == 1;
    }
}