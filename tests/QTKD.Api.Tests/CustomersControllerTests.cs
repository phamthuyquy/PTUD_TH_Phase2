using Microsoft.AspNetCore.Mvc;
using QTKD.Api.Application;
using QTKD.Api.Controllers;

namespace QTKD.Api.Tests;

public sealed class CustomersControllerTests
{
    [Fact]
    public async Task GetCustomers_ReturnsCustomerList()
    {
        IReadOnlyList<CustomerReadModel> customers =
        [
            new CustomerReadModel(1, "Công ty Minh Phát", "Active"),
            new CustomerReadModel(2, "Công ty An Bình", "Active")
        ];
        var controller = new CustomersController(new StubCustomerReadService(customers));

        var actionResult = await controller.GetCustomers(CancellationToken.None);

        var result = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsAssignableFrom<IReadOnlyList<CustomerReadModel>>(result.Value);
        Assert.Equal(2, response.Count);
        Assert.Equal("Công ty Minh Phát", response[0].Name);
    }

    private sealed class StubCustomerReadService(
        IReadOnlyList<CustomerReadModel> customers) : ICustomerService
    {
        public Task<IReadOnlyList<CustomerReadModel>> GetCustomersAsync(
            CancellationToken cancellationToken)
        {
            return Task.FromResult(customers);
        }

        public Task<CustomerResponse?> GetCustomerByIdAsync(
            int id,
            CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<ApiOperationResult<CustomerResponse>> CreateCustomerAsync(
            CreateCustomerRequest request,
            CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<ApiOperationResult<CustomerResponse>> UpdateCustomerAsync(
            int id,
            UpdateCustomerRequest request,
            CancellationToken cancellationToken) =>
            throw new NotImplementedException();
    }
}
