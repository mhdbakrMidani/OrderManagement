namespace OrderManagement.Application.DTOs.Customers;

public class CreateCustomerRequest
{
    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;
}