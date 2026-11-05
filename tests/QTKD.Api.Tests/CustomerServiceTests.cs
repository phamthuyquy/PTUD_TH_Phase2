using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using QTKD.Api.Application;
using QTKD.Api.Infrastructure;

namespace QTKD.Api.Tests;

public sealed class CustomerServiceTests
{
    [Fact]
    public async Task CreateCustomer_WhenValid_PersistsFullCustomerProfile()
    {
        await using var connection = await CreateOpenConnectionAsync();
        await using var dbContext = await CreateDbContextAsync(connection);
        var service = new CustomerService(dbContext);

        var result = await service.CreateCustomerAsync(
            new CreateCustomerRequest
            {
                Code = "CUS-003",
                Name = "Công ty Cảng Demo",
                TaxCode = "0310000003",
                Address = "Hải Phòng",
                RepresentativeName = "Lê Hải",
                ContactInfo = "hai@example.test",
                Status = "Active"
            },
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal("CUS-003", result.Value!.Code);
        Assert.Equal("0310000003", result.Value.TaxCode);
        Assert.Equal("Công ty Cảng Demo", (await dbContext.Customers
            .SingleAsync(customer => customer.Id == result.Value.Id)).Name);
    }

    [Fact]
    public async Task CreateCustomer_WhenCodeAlreadyExists_ReturnsConflict()
    {
        await using var connection = await CreateOpenConnectionAsync();
        await using var dbContext = await CreateDbContextAsync(connection);
        var service = new CustomerService(dbContext);

        var result = await service.CreateCustomerAsync(
            new CreateCustomerRequest
            {
                Code = "CUS-001",
                Name = "Công ty Trùng Mã",
                TaxCode = "0310000999",
                Address = "Hà Nội",
                RepresentativeName = "Người đại diện",
                ContactInfo = "duplicate@example.test",
                Status = "Active"
            },
            CancellationToken.None);

        Assert.Equal(StatusCodes.Status409Conflict, result.StatusCode);
    }

    private static async Task<SqliteConnection> CreateOpenConnectionAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        return connection;
    }

    private static async Task<QtkdDbContext> CreateDbContextAsync(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<QtkdDbContext>()
            .UseSqlite(connection)
            .Options;
        var dbContext = new QtkdDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();
        return dbContext;
    }
}
