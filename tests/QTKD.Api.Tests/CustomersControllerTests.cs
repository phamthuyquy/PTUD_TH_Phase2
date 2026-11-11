using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using QTKD.Api.Application;

namespace QTKD.Api.Tests
{
    [Trait("Category", "Integration")]
    public sealed class CustomersControllerTests : IDisposable
    {
        private readonly IntegrationApiFactory _factory;
        private readonly HttpClient _client;

        public CustomersControllerTests()
        {
            _factory = new IntegrationApiFactory();
            _client = _factory.CreateClient();
            _factory.EnsureDatabaseCreated();
        }

        [Fact]
        public async Task CreateCustomer_ValidPayload_CreatesAndCanBeReadBack_TC01()
        {
            var request = new CreateCustomerRequest
            {
                Code = $"KH-{Guid.NewGuid():N}"[..23],
                Name = "Công ty Minh Phát",
                TaxCode = Guid.NewGuid().ToString("N")[..30],
                Address = "TP. Hồ Chí Minh",
                RepresentativeName = "Nguyễn Văn A",
                ContactInfo = "0909123456",
                Status = "Active"
            };

            var createResponse = await _client.PostAsJsonAsync("/api/customers", request);

            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
            var createdCustomer = await createResponse.Content.ReadFromJsonAsync<CustomerResponse>();
            Assert.NotNull(createdCustomer);
            Assert.True(createdCustomer.Id > 0);
            Assert.Equal(request.Code, createdCustomer.Code);
            Assert.Equal(request.Name, createdCustomer.Name);
            Assert.Equal(request.Status, createdCustomer.Status);

            var readResponse = await _client.GetAsync($"/api/customers/{createdCustomer.Id}");

            Assert.Equal(HttpStatusCode.OK, readResponse.StatusCode);
            var readCustomer = await readResponse.Content.ReadFromJsonAsync<CustomerResponse>();
            Assert.NotNull(readCustomer);
            Assert.Equal(createdCustomer.Id, readCustomer.Id);
            Assert.Equal(createdCustomer.Code, readCustomer.Code);
            Assert.Equal(createdCustomer.Name, readCustomer.Name);
            Assert.Equal(createdCustomer.TaxCode, readCustomer.TaxCode);
        }

        [Fact]
        public async Task CreateCustomer_MissingRequiredName_IsRejectedWithValidationError_TC02()
        {
            var request = new CreateCustomerRequest
            {
                Code = $"KH-{Guid.NewGuid():N}"[..23],
                Name = " ",
                TaxCode = Guid.NewGuid().ToString("N")[..30],
                Address = "TP. Hồ Chí Minh",
                RepresentativeName = "Nguyễn Văn A",
                ContactInfo = "0909123456",
                Status = "Active"
            };

            var response = await _client.PostAsJsonAsync("/api/customers", request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            using var body = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync());
            Assert.True(body.RootElement.GetProperty("errors").TryGetProperty("Name", out _));
        }

        public void Dispose()
        {
            _client.Dispose();
            _factory.Dispose();
        }
    }
}