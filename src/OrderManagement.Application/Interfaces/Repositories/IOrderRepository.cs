using OrderManagement.Application.DTOs.Orders;
using OrderManagement.Domain.Entities;

namespace OrderManagement.Application.Interfaces.Repositories;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<OrderListResult> GetListAsync(
        OrderListRequest request,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Order order,
        CancellationToken cancellationToken = default);

    Task<bool> TryConfirmAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<bool> TryCancelAsync(
        int id,
        CancellationToken cancellationToken = default);
}