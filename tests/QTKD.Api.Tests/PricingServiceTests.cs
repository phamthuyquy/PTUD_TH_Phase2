using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using QTKD.Api.Application;
using QTKD.Api.Infrastructure;

namespace QTKD.Api.Tests;

public sealed class PricingServiceTests
{
    [Fact]
    public async Task CreatePriceItem_WhenPriceListExistsAndPriceIsPositive_CreatesItem()
    {
        await using var connection = await CreateOpenConnectionAsync();
        await using var dbContext = await CreateDbContextAsync(connection);
        var service = new PricingService(dbContext);

        var result = await service.CreatePriceItemAsync(
            1,
            new CreatePriceItemRequest { ServiceCode = "PORT-003", UnitPrice = 500000m },
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(1, result.Value!.PriceListId);
        Assert.Equal(500000m, result.Value.UnitPrice);
    }

    [Fact]
    public async Task CreatePriceItem_WhenPriceListDoesNotExist_ReturnsNotFound()
    {
        await using var connection = await CreateOpenConnectionAsync();
        await using var dbContext = await CreateDbContextAsync(connection);
        var service = new PricingService(dbContext);

        var result = await service.CreatePriceItemAsync(
            999,
            new CreatePriceItemRequest { ServiceCode = "PORT-003", UnitPrice = 500000m },
            CancellationToken.None);

        Assert.Equal(StatusCodes.Status404NotFound, result.StatusCode);
    }

    [Fact]
    public async Task CreatePriceList_WhenDatesAreReversed_ReturnsBadRequest()
    {
        await using var connection = await CreateOpenConnectionAsync();
        await using var dbContext = await CreateDbContextAsync(connection);
        var service = new PricingService(dbContext);

        var result = await service.CreatePriceListAsync(
            new CreatePriceListRequest
            {
                Name = "Invalid period",
                ContractId = 1,
                EffectiveDate = new DateOnly(2026, 12, 31),
                EndDate = new DateOnly(2026, 1, 1)
            },
            CancellationToken.None);

        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
    }

    [Fact]
    public async Task CreatePriceList_WhenContractExists_CreatesScopedPriceList()
    {
        await using var connection = await CreateOpenConnectionAsync();
        await using var dbContext = await CreateDbContextAsync(connection);
        var service = new PricingService(dbContext);

        var result = await service.CreatePriceListAsync(
            new CreatePriceListRequest
            {
                Name = "Bảng giá tháng 11",
                ContractId = 1,
                EffectiveDate = new DateOnly(2026, 11, 1),
                EndDate = new DateOnly(2026, 11, 30)
            },
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(1, result.Value!.ContractId);
        Assert.Equal(new DateOnly(2026, 11, 30), result.Value.EndDate);
    }

    [Fact]
    public async Task CreatePriceItem_WhenPriceIsNotPositive_ReturnsBadRequest()
    {
        await using var connection = await CreateOpenConnectionAsync();
        await using var dbContext = await CreateDbContextAsync(connection);
        var service = new PricingService(dbContext);

        var result = await service.CreatePriceItemAsync(
            1,
            new CreatePriceItemRequest { ServiceCode = "PORT-003", UnitPrice = 0m },
            CancellationToken.None);

        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
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
