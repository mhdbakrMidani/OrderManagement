using Microsoft.EntityFrameworkCore;
using OrderManagement.Infrastructure.Persistence;

namespace OrderManagement.Tests.Integration;

public static class OrderManagementDbContextFactory
{
    public static ApplicationDbContext Create()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(
                "Server=.;Database=OrderManagementDb;Trusted_Connection=True;TrustServerCertificate=True;")
            .Options;

        return new ApplicationDbContext(options);
    }
}