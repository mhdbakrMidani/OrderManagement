using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.DTOs.Orders;
using OrderManagement.Application.Exceptions;
using OrderManagement.Application.Interfaces;
using OrderManagement.Application.Interfaces.Repositories;
using OrderManagement.Application.Services.Orders;
using OrderManagement.Infrastructure.Persistence;
using OrderManagement.Infrastructure.Repositories;
using Xunit;

namespace OrderManagement.Tests.Integration;

public class OrderConcurrencyTests
{
    [Fact]
    public async Task CreateAsync_ShouldAllowOnlyOneOrder_WhenBuyingLastStock()
    {
        // Arrange
        await using var setupContext =
            OrderManagementDbContextFactory.Create();

        var customer = new Domain.Entities.Customer
        {
            Name = "Concurrency Test Customer",
            Email = $"concurrency-{Guid.NewGuid()}@test.com",
            CreatedAt = DateTime.UtcNow
        };

        var product = new Domain.Entities.Product
        {
            Name = "Concurrency Test Product",
            SKU = $"CON-{Guid.NewGuid():N}",
            Price = 100m,
            StockQuantity = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        setupContext.Customers.Add(customer);
        setupContext.Products.Add(product);

        await setupContext.SaveChangesAsync();

        var customerId = customer.Id;
        var productId = product.Id;

        // Act
        var task1 = CreateOrderAsync(
            customerId,
            productId);

        var task2 = CreateOrderAsync(
            customerId,
            productId);

        var results = await Task.WhenAll(
            WrapResultAsync(task1),
            WrapResultAsync(task2));

        // Assert
        Assert.Equal(
            1,
            results.Count(x => x.Success));

        Assert.Equal(
            1,
            results.Count(x => x.Exception is ConflictException));

        await using var verifyContext =
            OrderManagementDbContextFactory.Create();

        var finalProduct = await verifyContext.Products
            .AsNoTracking()
            .FirstAsync(x => x.Id == productId);

        Assert.Equal(0, finalProduct.StockQuantity);
    }

    private static async Task<OrderResponse> CreateOrderAsync(
        int customerId,
        int productId)
    {
        await using var context =
            OrderManagementDbContextFactory.Create();

        var customerRepository =
            new CustomerRepository(context);

        var productRepository =
            new ProductRepository(context);

        var productStockRepository =
            new ProductStockRepository(context);

        var orderRepository =
            new OrderRepository(context);

        var unitOfWork =
            new UnitOfWork(context);

        var service = new OrderService(
            customerRepository,
            productRepository,
            productStockRepository,
            orderRepository,
            unitOfWork);

        var request = new CreateOrderRequest
        {
            CustomerId = customerId,
            Items =
            [
                new CreateOrderItemRequest
                {
                    ProductId = productId,
                    Quantity = 1
                }
            ]
        };

        return await service.CreateAsync(request);
    }

    private static async Task<TestResult> WrapResultAsync(
        Task<OrderResponse> task)
    {
        try
        {
            var result = await task;

            return new TestResult
            {
                Success = true,
                Order = result
            };
        }
        catch (Exception exception)
        {
            return new TestResult
            {
                Success = false,
                Exception = exception
            };
        }
    }

    private class TestResult
    {
        public bool Success { get; set; }
        public OrderResponse? Order { get; set; }
        public Exception? Exception { get; set; }
    }
}