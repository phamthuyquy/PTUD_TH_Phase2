using Microsoft.EntityFrameworkCore;
using QTKD.Api.Application;
using QTKD.Api.Infrastructure;

namespace QTKD.Api.Tests
{
    [Trait("Category", "Unit")]
    public class CustomerServiceTests
    {
        private QtkdDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<QtkdDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new QtkdDbContext(options);
        }

        [Fact]
        public async Task CreateCustomer_WithValidData_ShouldSucceed_TC01()
        {
            // Arrange
            using var db = CreateInMemoryDbContext();
            var service = new CustomerService(db);
            var request = new CreateCustomerRequest
            {
                Code = "KH-001",
                Name = "Công ty Minh Phát",
                TaxCode = "0312345678",
                Address = "TP. Hồ Chí Minh",
                RepresentativeName = "Nguyễn Văn A",
                ContactInfo = "0909123456",
                Status = "Active"
            };

            var result = await service.CreateCustomerAsync(request, CancellationToken.None);

            // Assert
            Assert.True(result.Succeeded);
            Assert.True(result.Value!.Id > 0);
            var count = await db.Customers.CountAsync();
            Assert.Equal(1, count);
        }

        [Fact]
        public async Task CreateCustomer_WithoutName_ShouldFail_TC02()
        {
            // Arrange
            using var db = CreateInMemoryDbContext();
            var service = new CustomerService(db);
            var request = new CreateCustomerRequest
            {
                Code = "KH-002",
                Name = "",
                TaxCode = "0312345678",
                Address = "TP. Hồ Chí Minh",
                RepresentativeName = "Nguyễn Văn B",
                ContactInfo = "0909654321",
                Status = "Active"
            };

            var result = await service.CreateCustomerAsync(request, CancellationToken.None);

            // Assert
            Assert.False(result.Succeeded);
            var count = await db.Customers.CountAsync();
            Assert.Equal(0, count);
        }
    }
}