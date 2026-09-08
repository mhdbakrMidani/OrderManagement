namespace OrderManagement.Application.Interfaces.Repositories;

public interface IProductStockRepository
{
    Task<bool> DecreaseStockAsync(
        int productId,
        int quantity,
        CancellationToken cancellationToken = default);

    Task IncreaseStockAsync(
        int productId,
        int quantity,
        CancellationToken cancellationToken = default);
}