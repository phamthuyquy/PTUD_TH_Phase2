using Microsoft.AspNetCore.Mvc;
using QTKD.Api.Application;

namespace QTKD.Api.Controllers;

[ApiController]
[Route("api/contracts")]
public sealed class ContractsController(IContractReadService contractReadService) : ControllerBase
{
    [HttpGet("{id:int}")]
    [ProducesResponseType<ContractReadModel>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ContractReadModel>> GetContract(
        int id,
        CancellationToken cancellationToken)
    {
        var contract = await contractReadService.GetContractByIdAsync(id, cancellationToken);
        return contract is null ? NotFound() : Ok(contract);
    }
}
