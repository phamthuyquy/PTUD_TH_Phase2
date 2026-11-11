using Microsoft.EntityFrameworkCore;
using QTKD.Api.Application;
using QTKD.Api.Domain;
using QTKD.Api.Infrastructure;

namespace QTKD.Api.Tests
{
    [Trait("Category", "Service")]
    public class PricingServiceTests
    {
        private QtkdDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<QtkdDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new QtkdDbContext(options);
        }

        [Fact]
        public async Task CreatePriceList_WithValidData_ShouldSucceed()
        {
            // Arrange
            using var db = CreateInMemoryDbContext();
            var service = new PricingService(db);

            var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
            db.Contracts.Add(new Contract
            {
                Id = 1,
                ContractNo = "HD-TEST-001",
                CustomerId = 1,
                StartDate = today,
                EndDate = today.AddYears(1),
                Status = ContractStatuses.Draft
            });
            await db.SaveChangesAsync();

            var request = new CreatePriceListRequest
            {
                Name = "Bảng giá Dịch vụ Cảng 2026",
                ContractId = 1,
                EffectiveDate = today,
                EndDate = today.AddYears(1)
            };

            // Act
            var result = await service.CreatePriceListAsync(request, CancellationToken.None);

            // Assert
            Assert.True(result.Succeeded);
            var priceList = await db.PriceLists.FirstOrDefaultAsync();
            Assert.NotNull(priceList);
            Assert.Equal("Bảng giá Dịch vụ Cảng 2026", priceList.Name);
        }
    }
}