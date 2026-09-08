using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.DTOs.Orders;
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

    public async Task<OrderListResponse> GetListAsync(
        OrderListRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Orders
            .AsNoTracking()
            .Include(x => x.Items)
            .AsQueryable();

        var totalCount = await query.CountAsync(
            cancellationToken);

        var pageNumber = request.PageNumber < 1
            ? 1
            : request.PageNumber;

        var pageSize = request.PageSize < 1
            ? 10
            : Math.Min(request.PageSize, 100);

        var orders = await query
            .OrderByDescending(x => x.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var totalPages = (int)Math.Ceiling(
            totalCount / (double)pageSize);

        return new OrderListResponse
        {
            Items = orders
                .Select(MapToResponse)
                .ToList(),

            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };
    }

    private static OrderResponse MapToResponse(Order order)
    {
        return new OrderResponse
        {
            Id = order.Id,
            CustomerId = order.CustomerId,
            OrderDate = order.OrderDate,
            Status = order.Status,
            TotalAmount = order.TotalAmount,
            CreatedAt = order.CreatedAt,

            Items = order.Items
                .Select(item => new OrderItemResponse
                {
                    Id = item.Id,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    TotalPrice = item.TotalPrice
                })
                .ToList()
        };
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