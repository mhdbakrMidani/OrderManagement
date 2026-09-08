using OrderManagement.Domain.Entities;
using OrderManagement.Domain.Enums;

namespace OrderManagement.Application.Interfaces.Repositories;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(
        int id,
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