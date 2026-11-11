using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using QTKD.Api.Application;
using QTKD.Api.Domain;
using QTKD.Api.Infrastructure;

namespace QTKD.Api.Tests
{
    [Trait("Category", "Unit")]
    public class ContractCommandServiceTests: IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<QtkdDbContext> _options;

        public ContractCommandServiceTests()
        {
            // Mở kết nối SQLite In-Memory dùng chung trong suốt test case
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            _options = new DbContextOptionsBuilder<QtkdDbContext>()
                .UseSqlite(_connection)
                .Options;

            using var context = new QtkdDbContext(_options);
            context.Database.EnsureCreated();
        }

        public void Dispose()
        {
            _connection.Dispose();
        }

        private QtkdDbContext CreateDbContext() => new QtkdDbContext(_options);

        [Fact]
        public async Task UpdateContract_WhenStatusIsDraft_ShouldSucceed_TC03()
        {
            // Arrange
            using var db = CreateDbContext();
            var contract = new Contract
            {
                Id = 2,
                ContractNo = "HD-TEST-UPDATE",
                CustomerId = 1,
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow.Date),
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddYears(1)),
                Status = ContractStatuses.Draft
            };
            db.Contracts.Add(contract);
            await db.SaveChangesAsync();

            var service = new ContractCommandService(db, TimeProvider.System);
            var updateRequest = new UpdateContractRequest
            {
                ContractNo = "HD-TEST-UPDATE-REV1",
                CustomerId = contract.CustomerId,
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow.Date),
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddYears(2))
            };

            // Act
            var result = await service.UpdateContractAsync(contract.Id, updateRequest, CancellationToken.None);

            // Assert
            Assert.True(result.Succeeded);
            var updatedContract = await db.Contracts.FindAsync(contract.Id);
            Assert.NotNull(updatedContract);
            await db.Entry(updatedContract).ReloadAsync();
            Assert.Equal("HD-TEST-UPDATE-REV1", updatedContract.ContractNo);
        }

        [Fact]
        public async Task UpdateContract_WhenStatusIsPendingApproval_ShouldFail_CTR01_TC04()
        {
            // Arrange
            using var db = CreateDbContext();
            var contract = new Contract
            {
                Id = 2,
                ContractNo = "HD-TEST-PENDING",
                CustomerId = 1,
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow.Date),
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddYears(1)),
                Status = ContractStatuses.PendingApproval
            };
            db.Contracts.Add(contract);
            await db.SaveChangesAsync();

            var service = new ContractCommandService(db, TimeProvider.System);
            var updateRequest = new UpdateContractRequest
            {
                ContractNo = "HD-TEST-MODIFIED",
                CustomerId = contract.CustomerId,
                StartDate = contract.StartDate,
                EndDate = contract.EndDate
            };

            // Act
            var result = await service.UpdateContractAsync(contract.Id, updateRequest, CancellationToken.None);

            // Assert
            Assert.False(result.Succeeded);
            Assert.Equal("ContractNotEditable", result.Code);
            var dbContract = await db.Contracts.FindAsync(contract.Id);
            Assert.NotNull(dbContract);
            Assert.Equal("HD-TEST-PENDING", dbContract.ContractNo);
        }

        [Fact]
        public async Task UpdateContract_WhenStatusIsProvided_ShouldRejectDirectTransition_TC07()
        {
            using var db = CreateDbContext();
            var contract = new Contract
            {
                Id = 2,
                ContractNo = "HD-TEST-STATUS",
                CustomerId = 1,
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow.Date),
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddYears(1)),
                Status = ContractStatuses.Draft
            };
            db.Contracts.Add(contract);
            await db.SaveChangesAsync();

            var service = new ContractCommandService(db, TimeProvider.System);
            var request = new UpdateContractRequest
            {
                ContractNo = contract.ContractNo,
                CustomerId = contract.CustomerId,
                StartDate = contract.StartDate,
                EndDate = contract.EndDate,
                Status = ContractStatuses.Approved
            };

            var result = await service.UpdateContractAsync(
                contract.Id,
                request,
                CancellationToken.None);

            Assert.False(result.Succeeded);
            Assert.Equal("ContractStatusManagedByWorkflow", result.Code);
            var unchangedContract = await db.Contracts.FindAsync(contract.Id);
            Assert.NotNull(unchangedContract);
            Assert.Equal(ContractStatuses.Draft, unchangedContract.Status);
        }

        [Fact]
        public async Task SubmitContract_WhenMissingCustomerOrInvalidDates_ShouldFail_CTR02_TC05()
        {
            // Arrange
            using var db = CreateDbContext();
            var contract = new Contract
            {
                Id = 2,
                ContractNo = "HD-TEST-INVALID",
                CustomerId = 1,
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow.Date),
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-5)),
                ServiceTerms = "Dịch vụ thử nghiệm",
                Status = ContractStatuses.Draft
            };
            db.Contracts.Add(contract);
            await db.SaveChangesAsync();

            var service = new ContractCommandService(db, TimeProvider.System);

            // Act
            var result = await service.SubmitContractAsync(contract.Id, null, CancellationToken.None);

            // Assert
            Assert.False(result.Succeeded);
            Assert.Equal("ContractDoesNotMeetCtr02", result.Code);
            Assert.Contains("Contract effective dates must be ordered and not expired.", result.Errors!);
            var dbContract = await db.Contracts.FindAsync(contract.Id);
            Assert.NotNull(dbContract);
            Assert.Equal(ContractStatuses.Draft, dbContract.Status);
        }

        [Fact]
        public async Task SubmitContract_WhenValid_ShouldChangeStatusToPendingApproval_CTR03_TC06()
        {
            // Arrange
            using var db = CreateDbContext();
            var contract = new Contract
            {
                Id = 2,
                ContractNo = "HD-TEST-SUBMIT",
                CustomerId = 1,
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow.Date),
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddYears(1)),
                ServiceTerms = "Dịch vụ thử nghiệm",
                Status = ContractStatuses.Draft
            };
            db.Contracts.Add(contract);
            await db.SaveChangesAsync();

            var service = new ContractCommandService(db, TimeProvider.System);

            // Act
            var result = await service.SubmitContractAsync(contract.Id, null, CancellationToken.None);

            // Assert
            Assert.True(result.Succeeded);
            var dbContract = await db.Contracts.FindAsync(contract.Id);
            Assert.NotNull(dbContract);
            await db.Entry(dbContract).ReloadAsync();
            Assert.Equal(ContractStatuses.PendingApproval, dbContract.Status);
        }
    }
}