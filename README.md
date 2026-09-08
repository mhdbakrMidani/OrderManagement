# Order Management API

A production-oriented Order Management API built with ASP.NET Core and Entity Framework Core.

The project manages customers, products, inventory, and orders, with a focus on transactional consistency, concurrency safety, validation, and maintainable architecture.

## Features

* Customer management
* Product management
* Product search, filtering, sorting, and pagination
* Order creation and retrieval
* Order confirmation and cancellation
* Inventory stock management
* Transactional order creation
* Concurrency-safe stock updates
* Validation and centralized error handling
* ProblemDetails-based API errors
* Automated unit and integration tests

## Technology Stack

* .NET 10
* ASP.NET Core Web API
* Entity Framework Core
* SQL Server
* Swagger / OpenAPI
* xUnit
* Moq

## Architecture

The solution follows a Clean Architecture approach with clear separation of concerns:

```text
src/
├── OrderManagement.Api
├── OrderManagement.Application
├── OrderManagement.Domain
└── OrderManagement.Infrastructure

tests/
└── OrderManagement.Tests
```

### Layers

**Domain**

Contains the core business entities and enums without dependencies on infrastructure or frameworks.

**Application**

Contains application services, DTOs, repository abstractions, business rules, mappings, and application exceptions.

**Infrastructure**

Contains Entity Framework Core, SQL Server configuration, database migrations, repositories, transactions, and persistence-related implementations.

**API**

Contains controllers, middleware, dependency injection configuration, and HTTP-specific concerns.

**Tests**

Contains unit tests and an integration test covering concurrent order creation.

## API Endpoints

### Customers

```text
POST   /api/customers
GET    /api/customers/{id}
```

### Products

```text
POST   /api/products
GET    /api/products
GET    /api/products/{id}
```

Product listing supports:

* Pagination
* Search by name or SKU
* Active/inactive filtering
* Sorting by price or name

Example:

```text
GET /api/products?pageNumber=1&pageSize=20&search=phone&isActive=true&sortBy=price
```

### Orders

```text
POST   /api/orders
GET    /api/orders/{id}
GET    /api/orders
POST   /api/orders/{id}/confirm
POST   /api/orders/{id}/cancel
```

## Order Creation

Creating an order performs the following validations and operations:

1. Verify that the customer exists.
2. Verify that all requested products exist.
3. Verify that all products are active.
4. Validate requested quantities.
5. Read the current product price from the database.
6. Store the current price as the order item's `UnitPrice`.
7. Calculate the order total.
8. Decrease product stock.
9. Create the order and order items.
10. Commit the entire operation within a database transaction.

If any step fails, the transaction is rolled back so the database is not left in a partially updated state.

## Concurrency Strategy

Inventory updates use an atomic database-level conditional update.

Stock is decreased only when the product is active and has sufficient stock:

```text
StockQuantity >= requested quantity
```

The stock update and the decrement are performed as a single database operation.

For example, if the stock is `1` and two requests attempt to purchase one unit simultaneously:

```text
Request A → Stock = 1 → Update succeeds → Stock = 0
Request B → Stock = 0 → Condition fails → Conflict
```

This prevents negative stock and ensures that the same inventory unit cannot be successfully consumed by two concurrent orders.

Order confirmation and cancellation also use conditional database updates based on the current order status, making them safe when multiple requests attempt to change the same order simultaneously.

## Order Cancellation

An order can only be cancelled while its status is `Pending`.

When a pending order is cancelled, the stock previously deducted for its order items is restored.

The cancellation and stock restoration are performed within the same database transaction to keep the operation atomic.

## Order Status

Orders support the following statuses:

```text
Pending
Confirmed
Cancelled
Completed
```

An order can only be confirmed when its current status is `Pending`.

In this implementation, cancellation is allowed only from the `Pending` state.

## Database

Entity Framework Core with SQL Server is used for persistence.

The database includes:

* Primary keys
* Foreign keys
* Unique constraints
* Indexes
* Proper entity relationships
* Decimal precision for monetary values
* EF Core migrations

Unique constraints are enforced at the database level for values such as product SKU and customer email.

## Validation & Error Handling

The API uses:

* ASP.NET Core model validation
* Business-level validation in the application layer
* Centralized exception handling middleware
* RFC 7807-style `ProblemDetails` responses

Examples of HTTP responses include:

```text
400 Bad Request
404 Not Found
409 Conflict
500 Internal Server Error
```

Duplicate unique-key violations are returned as `409 Conflict` without exposing database-specific exception details.

## Testing

The test project contains unit tests covering important order scenarios, including:

* Successful order creation
* Insufficient stock
* Duplicate products in an order
* Missing customer
* Missing product
* Inactive product
* Order confirmation
* Invalid order confirmation
* Order cancellation
* Invalid order cancellation

An integration test also verifies concurrent purchases of the last available product unit and ensures that only one request succeeds.

## Running the Project

### Prerequisites

* .NET 10 SDK
* SQL Server

### Database

Update the connection string in:

```text
OrderManagement.Api/appsettings.json
```

Then apply the EF Core migrations.

The project includes an EF Core design-time `DbContext` factory for migrations.

### Run

Open the solution in Visual Studio and run the `OrderManagement.Api` project.

Swagger is available in the Development environment.

## Project Structure

```text
OrderManagement
│
├── src
│   ├── OrderManagement.Api
│   │   ├── Controllers
│   │   └── Middleware
│   │
│   ├── OrderManagement.Application
│   │   ├── DTOs
│   │   ├── Exceptions
│   │   ├── Interfaces
│   │   ├── Mappings
│   │   └── Services
│   │
│   ├── OrderManagement.Domain
│   │   ├── Entities
│   │   └── Enums
│   │
│   └── OrderManagement.Infrastructure
│       ├── DependencyInjection
│       ├── Persistence
│       └── Repositories
│
└── tests
    └── OrderManagement.Tests
```

## Notes

The implementation intentionally focuses on the requirements of the assessment while keeping the solution simple, maintainable, and production-oriented.
