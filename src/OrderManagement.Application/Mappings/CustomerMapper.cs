using OrderManagement.Application.DTOs.Customers;
using OrderManagement.Domain.Entities;

namespace OrderManagement.Application.Mappings;

public static class CustomerMapper
{
    public static CustomerResponse ToResponse(Customer customer)
    {
        return new CustomerResponse
        {
            Id = customer.Id,
            Name = customer.Name,
            Email = customer.Email,
            CreatedAt = customer.CreatedAt
        };
    }
}