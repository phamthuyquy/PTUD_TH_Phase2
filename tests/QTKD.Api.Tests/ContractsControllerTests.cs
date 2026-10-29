using Microsoft.AspNetCore.Mvc;
using QTKD.Api.Application;
using QTKD.Api.Controllers;

namespace QTKD.Api.Tests;

public sealed class ContractsControllerTests
{
    [Fact]
    public async Task GetContract_WhenContractExists_ReturnsContract()
    {
        var contract = new ContractReadModel(
            1,
            "HD-2026-018",
            1,
            "Công ty Minh Phát",
            new DateOnly(2026, 10, 1),
            new DateOnly(2027, 9, 30),
            "Draft");
        var controller = new ContractsController(new StubContractReadService(contract));

        var actionResult = await controller.GetContract(1, CancellationToken.None);

        var result = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<ContractReadModel>(result.Value);
        Assert.Equal(contract, response);
    }

    [Fact]
    public async Task GetContract_WhenContractDoesNotExist_ReturnsNotFound()
    {
        var controller = new ContractsController(new StubContractReadService(null));

        var actionResult = await controller.GetContract(999, CancellationToken.None);

        Assert.IsType<NotFoundResult>(actionResult.Result);
    }

    private sealed class StubContractReadService(
        ContractReadModel? contract) : IContractReadService
    {
        public Task<ContractReadModel?> GetContractByIdAsync(
            int id,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(contract);
        }
    }
}
