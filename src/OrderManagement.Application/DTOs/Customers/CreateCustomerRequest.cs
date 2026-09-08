using System.ComponentModel.DataAnnotations;

namespace OrderManagement.Application.DTOs.Customers;

public class CreateCustomerRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = null!;

    [Required]
    [EmailAddress]
    [MaxLength(320)]
    public string Email { get; set; } = null!;
}