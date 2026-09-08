using OrderManagement.Application.DTOs.Orders;
using OrderManagement.Application.Exceptions;
using OrderManagement.Application.Interfaces;
using OrderManagement.Application.Interfaces.Repositories;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.Enums;

namespace OrderManagement.Application.Services.Orders;

public class OrderService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IProductRepository _productRepository;
    private readonly IProductStockRepository _productStockRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;

    public OrderService(
        ICustomerRepository customerRepository,
        IProductRepository productRepository,
        IProductStockRepository productStockRepository,
        IOrderRepository orderRepository,
        IUnitOfWork unitOfWork)
    {
        _customerRepository = customerRepository;
        _productRepository = productRepository;
        _productStockRepository = productStockRepository;
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<OrderResponse> CreateAsync(
        CreateOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Items is null || request.Items.Count == 0)
        {
            throw new ValidationException(
                "Order must contain at least one item.");
        }

        if (request.Items.Any(x => x.Quantity <= 0))
        {
            throw new ValidationException(
                "Order item quantity must be greater than zero.");
        }

        if (request.Items
            .GroupBy(x => x.ProductId)
            .Any(x => x.Count() > 1))
        {
            throw new ValidationException(
                "The same product cannot be added more than once.");
        }

        var customerExists = await _customerRepository.ExistsAsync(
            request.CustomerId,
            cancellationToken);

        if (!customerExists)
        {
            throw new NotFoundException(
                "Customer was not found.");
        }

        var productIds = request.Items
            .Select(x => x.ProductId)
            .ToList();

        var products = await _productRepository.GetByIdsAsync(
            productIds,
            cancellationToken);

        if (products.Count != productIds.Count)
        {
            throw new NotFoundException(
                "One or more products were not found.");
        }

        if (products.Any(x => !x.IsActive))
        {
            throw new ConflictException(
                "One or more products are inactive.");
        }

        await using var transaction =
            await _unitOfWork.BeginTransactionAsync(
                cancellationToken);

        try
        {
            var now = DateTime.UtcNow;

            var order = new Order
            {
                CustomerId = request.CustomerId,
                OrderDate = now,
                Status = OrderStatus.Pending,
                CreatedAt = now
            };

            foreach (var itemRequest in request.Items)
            {
                var product = products.First(
                    x => x.Id == itemRequest.ProductId);

                var stockDecreased =
                    await _productStockRepository.DecreaseStockAsync(
                        product.Id,
                        itemRequest.Quantity,
                        cancellationToken);

                if (!stockDecreased)
                {
                    throw new ConflictException(
                        $"Insufficient stock for product '{product.Name}'.");
                }

                var totalPrice =
                    product.Price * itemRequest.Quantity;

                var orderItem = new OrderItem
                {
                    ProductId = product.Id,
                    Quantity = itemRequest.Quantity,
                    UnitPrice = product.Price,
                    TotalPrice = totalPrice
                };

                order.Items.Add(orderItem);
            }

            order.TotalAmount = order.Items.Sum(
                x => x.TotalPrice);

            await _orderRepository.AddAsync(
                order,
                cancellationToken);

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);

            await transaction.CommitAsync(
                cancellationToken);

            return MapToResponse(order);
        }
        catch
        {
            await transaction.RollbackAsync(
                cancellationToken);

            throw;
        }
    }

    public async Task<OrderResponse> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdAsync(
            id,
            cancellationToken);

        if (order is null)
        {
            throw new NotFoundException(
                "Order was not found.");
        }

        return MapToResponse(order);
    }

    public async Task ConfirmAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var orderExists = await _orderRepository.GetByIdAsync(
            id,
            cancellationToken);

        if (orderExists is null)
        {
            throw new NotFoundException(
                "Order was not found.");
        }

        var confirmed = await _orderRepository.TryConfirmAsync(
            id,
            cancellationToken);

        if (!confirmed)
        {
            throw new ConflictException(
                "Order cannot be confirmed because it is no longer pending.");
        }
    }

    public async Task CancelAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdAsync(
            id,
            cancellationToken);

        if (order is null)
        {
            throw new NotFoundException(
                "Order was not found.");
        }

        await using var transaction =
            await _unitOfWork.BeginTransactionAsync(
                cancellationToken);

        try
        {
            var cancelled = await _orderRepository.TryCancelAsync(
                id,
                cancellationToken);

            if (!cancelled)
            {
                throw new ConflictException(
                    "Order cannot be cancelled because it is no longer pending.");
            }

            foreach (var item in order.Items)
            {
                await _productStockRepository.IncreaseStockAsync(
                    item.ProductId,
                    item.Quantity,
                    cancellationToken);
            }

            await transaction.CommitAsync(
                cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(
                cancellationToken);

            throw;
        }
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
}