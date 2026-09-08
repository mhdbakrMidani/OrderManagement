using Moq;
using OrderManagement.Application.DTOs.Orders;
using OrderManagement.Application.Exceptions;
using OrderManagement.Application.Interfaces;
using OrderManagement.Application.Interfaces.Repositories;
using OrderManagement.Application.Services.Orders;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.Enums;
using Xunit;

namespace OrderManagement.Tests.Orders;

public class OrderServiceTests
{
    [Fact]
    public async Task CreateAsync_ShouldThrowConflict_WhenStockIsInsufficient()
    {
        // Arrange
        var customerRepository = new Mock<ICustomerRepository>();
        var productRepository = new Mock<IProductRepository>();
        var productStockRepository = new Mock<IProductStockRepository>();
        var orderRepository = new Mock<IOrderRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        customerRepository
            .Setup(x => x.ExistsAsync(
                1,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var product = new Product
        {
            Id = 1,
            Name = "Laptop",
            SKU = "LAP-001",
            Price = 850m,
            StockQuantity = 2,
            IsActive = true
        };

        productRepository
            .Setup(x => x.GetByIdsAsync(
                It.IsAny<IEnumerable<int>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { product });

        productStockRepository
            .Setup(x => x.DecreaseStockAsync(
                1,
                5,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var transaction = new Mock<ITransaction>();

        unitOfWork
            .Setup(x => x.BeginTransactionAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction.Object);

        var service = new OrderService(
            customerRepository.Object,
            productRepository.Object,
            productStockRepository.Object,
            orderRepository.Object,
            unitOfWork.Object);

        var request = new CreateOrderRequest
        {
            CustomerId = 1,
            Items =
            [
                new CreateOrderItemRequest
                {
                    ProductId = 1,
                    Quantity = 5
                }
            ]
        };

        // Act
        var exception = await Assert.ThrowsAsync<ConflictException>(
            () => service.CreateAsync(request));

        // Assert
        Assert.Contains(
            "Insufficient stock",
            exception.Message);

        productStockRepository.Verify(
            x => x.DecreaseStockAsync(
                1,
                5,
                It.IsAny<CancellationToken>()),
            Times.Once);

        orderRepository.Verify(
            x => x.AddAsync(
                It.IsAny<Order>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        unitOfWork.Verify(
            x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);

        transaction.Verify(
            x => x.RollbackAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateOrder_WhenStockIsAvailable()
    {
        // Arrange
        var customerRepository = new Mock<ICustomerRepository>();
        var productRepository = new Mock<IProductRepository>();
        var productStockRepository = new Mock<IProductStockRepository>();
        var orderRepository = new Mock<IOrderRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        customerRepository
            .Setup(x => x.ExistsAsync(
                1,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var product = new Product
        {
            Id = 1,
            Name = "Laptop",
            SKU = "LAP-001",
            Price = 850m,
            StockQuantity = 10,
            IsActive = true
        };

        productRepository
            .Setup(x => x.GetByIdsAsync(
                It.IsAny<IEnumerable<int>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { product });

        productStockRepository
            .Setup(x => x.DecreaseStockAsync(
                1,
                2,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var transaction = new Mock<ITransaction>();

        unitOfWork
            .Setup(x => x.BeginTransactionAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction.Object);

        Order? createdOrder = null;

        orderRepository
            .Setup(x => x.AddAsync(
                It.IsAny<Order>(),
                It.IsAny<CancellationToken>()))
            .Callback<Order, CancellationToken>((order, _) =>
            {
                createdOrder = order;
            })
            .Returns(Task.CompletedTask);

        unitOfWork
            .Setup(x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var service = new OrderService(
            customerRepository.Object,
            productRepository.Object,
            productStockRepository.Object,
            orderRepository.Object,
            unitOfWork.Object);

        var request = new CreateOrderRequest
        {
            CustomerId = 1,
            Items =
            [
                new CreateOrderItemRequest
            {
                ProductId = 1,
                Quantity = 2
            }
            ]
        };

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        Assert.NotNull(createdOrder);

        Assert.Equal(1, result.CustomerId);
        Assert.Equal(OrderStatus.Pending, result.Status);

        Assert.Equal(1700m, result.TotalAmount);

        Assert.Single(result.Items);

        var item = result.Items[0];

        Assert.Equal(1, item.ProductId);
        Assert.Equal(2, item.Quantity);
        Assert.Equal(850m, item.UnitPrice);
        Assert.Equal(1700m, item.TotalPrice);

        productStockRepository.Verify(
            x => x.DecreaseStockAsync(
                1,
                2,
                It.IsAny<CancellationToken>()),
            Times.Once);

        orderRepository.Verify(
            x => x.AddAsync(
                It.IsAny<Order>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        unitOfWork.Verify(
            x => x.SaveChangesAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);

        transaction.Verify(
            x => x.CommitAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowValidation_WhenSameProductIsAddedMoreThanOnce()
    {
        // Arrange
        var customerRepository = new Mock<ICustomerRepository>();
        var productRepository = new Mock<IProductRepository>();
        var productStockRepository = new Mock<IProductStockRepository>();
        var orderRepository = new Mock<IOrderRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        var service = new OrderService(
            customerRepository.Object,
            productRepository.Object,
            productStockRepository.Object,
            orderRepository.Object,
            unitOfWork.Object);

        var request = new CreateOrderRequest
        {
            CustomerId = 1,
            Items =
            [
                new CreateOrderItemRequest
            {
                ProductId = 1,
                Quantity = 2
            },
            new CreateOrderItemRequest
            {
                ProductId = 1,
                Quantity = 3
            }
            ]
        };

        // Act
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => service.CreateAsync(request));

        // Assert
        Assert.Contains(
            "same product",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);

        customerRepository.Verify(
            x => x.ExistsAsync(
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        productRepository.Verify(
            x => x.GetByIdsAsync(
                It.IsAny<IEnumerable<int>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        productStockRepository.Verify(
            x => x.DecreaseStockAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ConfirmAsync_ShouldConfirmOrder_WhenOrderIsPending()
    {
        // Arrange
        var orderRepository = new Mock<IOrderRepository>();

        var pendingOrder = new Order
        {
            Id = 1,
            CustomerId = 1,
            Status = OrderStatus.Pending
        };

        orderRepository
            .Setup(x => x.GetByIdAsync(
                1,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(pendingOrder);

        orderRepository
            .Setup(x => x.TryConfirmAsync(
                1,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new OrderService(
            new Mock<ICustomerRepository>().Object,
            new Mock<IProductRepository>().Object,
            new Mock<IProductStockRepository>().Object,
            orderRepository.Object,
            new Mock<IUnitOfWork>().Object);

        // Act
        await service.ConfirmAsync(1);

        // Assert
        orderRepository.Verify(
            x => x.TryConfirmAsync(
                1,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ConfirmAsync_ShouldThrowConflict_WhenOrderIsAlreadyConfirmed()
    {
        // Arrange
        var orderRepository = new Mock<IOrderRepository>();

        var confirmedOrder = new Order
        {
            Id = 1,
            CustomerId = 1,
            Status = OrderStatus.Confirmed
        };

        orderRepository
            .Setup(x => x.GetByIdAsync(
                1,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(confirmedOrder);

        orderRepository
            .Setup(x => x.TryConfirmAsync(
                1,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var service = new OrderService(
            new Mock<ICustomerRepository>().Object,
            new Mock<IProductRepository>().Object,
            new Mock<IProductStockRepository>().Object,
            orderRepository.Object,
            new Mock<IUnitOfWork>().Object);

        // Act
        var exception = await Assert.ThrowsAsync<ConflictException>(
            () => service.ConfirmAsync(1));

        // Assert
        Assert.Contains(
            "no longer pending",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);

        orderRepository.Verify(
            x => x.TryConfirmAsync(
                1,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CancelAsync_ShouldRestoreStock_WhenOrderIsPending()
    {
        // Arrange
        var orderRepository = new Mock<IOrderRepository>();
        var productStockRepository = new Mock<IProductStockRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        var pendingOrder = new Order
        {
            Id = 1,
            CustomerId = 1,
            Status = OrderStatus.Pending,
            Items =
            [
                new OrderItem
            {
                Id = 1,
                ProductId = 10,
                Quantity = 2
            },
            new OrderItem
            {
                Id = 2,
                ProductId = 20,
                Quantity = 3
            }
            ]
        };

        orderRepository
            .Setup(x => x.GetByIdAsync(
                1,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(pendingOrder);

        orderRepository
            .Setup(x => x.TryCancelAsync(
                1,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var transaction = new Mock<ITransaction>();

        unitOfWork
            .Setup(x => x.BeginTransactionAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction.Object);

        var service = new OrderService(
            new Mock<ICustomerRepository>().Object,
            new Mock<IProductRepository>().Object,
            productStockRepository.Object,
            orderRepository.Object,
            unitOfWork.Object);

        // Act
        await service.CancelAsync(1);

        // Assert
        productStockRepository.Verify(
            x => x.IncreaseStockAsync(
                10,
                2,
                It.IsAny<CancellationToken>()),
            Times.Once);

        productStockRepository.Verify(
            x => x.IncreaseStockAsync(
                20,
                3,
                It.IsAny<CancellationToken>()),
            Times.Once);

        transaction.Verify(
            x => x.CommitAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CancelAsync_ShouldThrowConflict_WhenOrderIsAlreadyConfirmed()
    {
        // Arrange
        var orderRepository = new Mock<IOrderRepository>();
        var productStockRepository = new Mock<IProductStockRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        var confirmedOrder = new Order
        {
            Id = 1,
            CustomerId = 1,
            Status = OrderStatus.Confirmed,
            Items =
            [
                new OrderItem
            {
                Id = 1,
                ProductId = 10,
                Quantity = 2
            }
            ]
        };

        orderRepository
            .Setup(x => x.GetByIdAsync(
                1,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(confirmedOrder);

        orderRepository
            .Setup(x => x.TryCancelAsync(
                1,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var transaction = new Mock<ITransaction>();

        unitOfWork
            .Setup(x => x.BeginTransactionAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction.Object);

        var service = new OrderService(
            new Mock<ICustomerRepository>().Object,
            new Mock<IProductRepository>().Object,
            productStockRepository.Object,
            orderRepository.Object,
            unitOfWork.Object);

        // Act
        var exception = await Assert.ThrowsAsync<ConflictException>(
            () => service.CancelAsync(1));

        // Assert
        Assert.Contains(
            "no longer pending",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);

        productStockRepository.Verify(
            x => x.IncreaseStockAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        transaction.Verify(
            x => x.CommitAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);

        transaction.Verify(
            x => x.RollbackAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowNotFound_WhenCustomerDoesNotExist()
    {
        // Arrange
        var customerRepository = new Mock<ICustomerRepository>();
        var productRepository = new Mock<IProductRepository>();
        var productStockRepository = new Mock<IProductStockRepository>();
        var orderRepository = new Mock<IOrderRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        customerRepository
            .Setup(x => x.ExistsAsync(
                999,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var service = new OrderService(
            customerRepository.Object,
            productRepository.Object,
            productStockRepository.Object,
            orderRepository.Object,
            unitOfWork.Object);

        var request = new CreateOrderRequest
        {
            CustomerId = 999,
            Items =
            [
                new CreateOrderItemRequest
            {
                ProductId = 1,
                Quantity = 1
            }
            ]
        };

        // Act
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => service.CreateAsync(request));

        // Assert
        Assert.Contains(
            "Customer was not found",
            exception.Message);

        productRepository.Verify(
            x => x.GetByIdsAsync(
                It.IsAny<IEnumerable<int>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        productStockRepository.Verify(
            x => x.DecreaseStockAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        orderRepository.Verify(
            x => x.AddAsync(
                It.IsAny<Order>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowNotFound_WhenProductDoesNotExist()
    {
        // Arrange
        var customerRepository = new Mock<ICustomerRepository>();
        var productRepository = new Mock<IProductRepository>();
        var productStockRepository = new Mock<IProductStockRepository>();
        var orderRepository = new Mock<IOrderRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        customerRepository
            .Setup(x => x.ExistsAsync(
                1,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        productRepository
            .Setup(x => x.GetByIdsAsync(
                It.IsAny<IEnumerable<int>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>());

        var service = new OrderService(
            customerRepository.Object,
            productRepository.Object,
            productStockRepository.Object,
            orderRepository.Object,
            unitOfWork.Object);

        var request = new CreateOrderRequest
        {
            CustomerId = 1,
            Items =
            [
                new CreateOrderItemRequest
            {
                ProductId = 999,
                Quantity = 1
            }
            ]
        };

        // Act
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => service.CreateAsync(request));

        // Assert
        Assert.Contains(
            "products were not found",
            exception.Message);

        productStockRepository.Verify(
            x => x.DecreaseStockAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        orderRepository.Verify(
            x => x.AddAsync(
                It.IsAny<Order>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowConflict_WhenProductIsInactive()
    {
        // Arrange
        var customerRepository = new Mock<ICustomerRepository>();
        var productRepository = new Mock<IProductRepository>();
        var productStockRepository = new Mock<IProductStockRepository>();
        var orderRepository = new Mock<IOrderRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();

        customerRepository
            .Setup(x => x.ExistsAsync(
                1,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var inactiveProduct = new Product
        {
            Id = 1,
            Name = "Laptop",
            SKU = "LAP-001",
            Price = 850m,
            StockQuantity = 10,
            IsActive = false
        };

        productRepository
            .Setup(x => x.GetByIdsAsync(
                It.IsAny<IEnumerable<int>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { inactiveProduct });

        var service = new OrderService(
            customerRepository.Object,
            productRepository.Object,
            productStockRepository.Object,
            orderRepository.Object,
            unitOfWork.Object);

        var request = new CreateOrderRequest
        {
            CustomerId = 1,
            Items =
            [
                new CreateOrderItemRequest
            {
                ProductId = 1,
                Quantity = 1
            }
            ]
        };

        // Act
        var exception = await Assert.ThrowsAsync<ConflictException>(
            () => service.CreateAsync(request));

        // Assert
        Assert.Contains(
            "inactive",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);

        productStockRepository.Verify(
            x => x.DecreaseStockAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        orderRepository.Verify(
            x => x.AddAsync(
                It.IsAny<Order>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        unitOfWork.Verify(
            x => x.BeginTransactionAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}