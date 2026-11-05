using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using QTKD.Api.Application;
using QTKD.Api.Domain;
using QTKD.Api.Infrastructure;

namespace QTKD.Api.Tests;

public sealed class ContractCommandServiceTests
{
    [Fact]
    public async Task SubmitContract_WhenServiceTermsMissing_ReturnsCtr02ErrorsWithoutChangingData()
    {
        await using var connection = await CreateOpenConnectionAsync();
        await using var dbContext = await CreateDbContextAsync(connection);
        var contract = await dbContext.Contracts.SingleAsync();
        contract.ServiceTerms = null;
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var result = await service.SubmitContractAsync(
            contract.Id,
            "submit-missing-terms",
            CancellationToken.None);

        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
        Assert.Contains("ServiceTerms", Assert.Single(result.Errors!, error => error.Contains("ServiceTerms")));
        Assert.Equal(ContractStatuses.Draft, (await dbContext.Contracts.SingleAsync()).Status);
        Assert.Empty(await dbContext.ContractSubmissions.ToListAsync());
    }

    [Fact]
    public async Task SubmitContract_WhenCustomerIsSuspended_ReturnsCtr02Error()
    {
        await using var connection = await CreateOpenConnectionAsync();
        await using var dbContext = await CreateDbContextAsync(connection);
        var customer = await dbContext.Customers.SingleAsync(item => item.Id == 1);
        customer.Status = "Suspended";
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var result = await service.SubmitContractAsync(1, null, CancellationToken.None);

        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
        Assert.Contains(result.Errors!, error => error.Contains("Active status"));
        Assert.Equal(ContractStatuses.Draft, (await dbContext.Contracts.SingleAsync()).Status);
    }

    [Fact]
    public async Task SubmitContract_WhenEffectivePeriodIsExpired_ReturnsCtr02Error()
    {
        await using var connection = await CreateOpenConnectionAsync();
        await using var dbContext = await CreateDbContextAsync(connection);
        var contract = await dbContext.Contracts.SingleAsync();
        contract.EndDate = new DateOnly(2026, 10, 30);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var result = await service.SubmitContractAsync(1, null, CancellationToken.None);

        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
        Assert.Contains(result.Errors!, error => error.Contains("effective dates"));
    }

    [Fact]
    public async Task SubmitContract_WhenValid_TransitionsOnceAndReplaysSameSubmission()
    {
        await using var connection = await CreateOpenConnectionAsync();
        await using var dbContext = await CreateDbContextAsync(connection);
        var service = CreateService(dbContext);

        var first = await service.SubmitContractAsync(1, "submit-valid-1", CancellationToken.None);
        var replay = await service.SubmitContractAsync(1, "submit-valid-1", CancellationToken.None);
        var duplicate = await service.SubmitContractAsync(1, "submit-valid-2", CancellationToken.None);

        Assert.True(first.Succeeded);
        Assert.Equal(ContractStatuses.PendingApproval, first.Value!.Status);
        Assert.False(first.Value.IsReplay);
        Assert.True(replay.Succeeded);
        Assert.True(replay.Value!.IsReplay);
        Assert.Equal(first.Value.ApprovalId, replay.Value.ApprovalId);
        Assert.Equal(StatusCodes.Status409Conflict, duplicate.StatusCode);
        Assert.Equal(ContractStatuses.PendingApproval, (await dbContext.Contracts.SingleAsync()).Status);
        Assert.Single(await dbContext.ContractSubmissions.ToListAsync());
    }

    [Fact]
    public async Task SubmitContract_WhenWrongState_ReturnsConflictWithoutCreatingSubmission()
    {
        await using var connection = await CreateOpenConnectionAsync();
        await using var dbContext = await CreateDbContextAsync(connection);
        var contract = await dbContext.Contracts.SingleAsync();
        contract.Status = ContractStatuses.Approved;
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var result = await service.SubmitContractAsync(1, null, CancellationToken.None);

        Assert.Equal(StatusCodes.Status409Conflict, result.StatusCode);
        Assert.Equal(ContractStatuses.Approved, (await dbContext.Contracts.SingleAsync()).Status);
        Assert.Empty(await dbContext.ContractSubmissions.ToListAsync());
    }

    [Fact]
    public async Task UpdateContract_WhenNotEditable_ReturnsConflictAndLeavesFieldsUnchanged()
    {
        await using var connection = await CreateOpenConnectionAsync();
        await using var dbContext = await CreateDbContextAsync(connection);
        var contract = await dbContext.Contracts.SingleAsync();
        contract.Status = ContractStatuses.PendingApproval;
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var result = await service.UpdateContractAsync(
            contract.Id,
            new UpdateContractRequest
            {
                ContractNo = "CHANGED",
                CustomerId = 1,
                StartDate = contract.StartDate,
                EndDate = contract.EndDate,
                ServiceTerms = "Updated terms"
            },
            CancellationToken.None);

        Assert.Equal(StatusCodes.Status409Conflict, result.StatusCode);
        var storedContract = await dbContext.Contracts.SingleAsync();
        Assert.Equal("HD-2026-018", storedContract.ContractNo);
        Assert.Equal(ContractStatuses.PendingApproval, storedContract.Status);
    }

    [Fact]
    public async Task CreateContract_WhenCustomerIsActive_CreatesDraft()
    {
        await using var connection = await CreateOpenConnectionAsync();
        await using var dbContext = await CreateDbContextAsync(connection);
        var service = CreateService(dbContext);

        var result = await service.CreateContractAsync(
            new CreateContractRequest
            {
                ContractNo = "HD-NEW-001",
                CustomerId = 1,
                StartDate = new DateOnly(2026, 11, 1),
                EndDate = new DateOnly(2027, 10, 31),
                ServiceTerms = null
            },
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(ContractStatuses.Draft, result.Value!.Status);
        Assert.Equal("HD-NEW-001", result.Value.ContractNo);
    }

    [Fact]
    public async Task UpdateContract_WhenRevisionRequested_IsAllowed()
    {
        await using var connection = await CreateOpenConnectionAsync();
        await using var dbContext = await CreateDbContextAsync(connection);
        var contract = await dbContext.Contracts.SingleAsync();
        contract.Status = ContractStatuses.RevisionRequested;
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        var result = await service.UpdateContractAsync(
            contract.Id,
            new UpdateContractRequest
            {
                ContractNo = contract.ContractNo,
                CustomerId = contract.CustomerId,
                StartDate = contract.StartDate,
                EndDate = contract.EndDate,
                ServiceTerms = "Terms after revision."
            },
            CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal("Terms after revision.", result.Value!.ServiceTerms);
        Assert.Equal(ContractStatuses.RevisionRequested, result.Value.Status);
    }

    private static ContractCommandService CreateService(QtkdDbContext dbContext) =>
        new(dbContext, new FixedTimeProvider(new DateTimeOffset(2026, 10, 31, 0, 0, 0, TimeSpan.Zero)));

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

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
