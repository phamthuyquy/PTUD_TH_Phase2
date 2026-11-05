using Microsoft.AspNetCore.Mvc;
using QTKD.Api.Application;

namespace QTKD.Api.Controllers;

[ApiController]
[Route("api/contracts")]
public sealed class ContractsController(
    IContractReadService contractReadService,
    IContractCommandService contractCommandService) : ControllerBase
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

    [HttpPost]
    [ProducesResponseType<ContractReadModel>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateContract(
        CreateContractRequest request,
        CancellationToken cancellationToken)
    {
        var result = await contractCommandService.CreateContractAsync(request, cancellationToken);
        if (!result.Succeeded)
        {
            return Problem(
                statusCode: result.StatusCode,
                title: result.Code,
                detail: result.Message);
        }

        return CreatedAtAction(
            nameof(GetContract),
            new { id = result.Value!.Id },
            result.Value);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType<ContractReadModel>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateContract(
        int id,
        UpdateContractRequest request,
        CancellationToken cancellationToken)
    {
        var result = await contractCommandService.UpdateContractAsync(id, request, cancellationToken);
        if (!result.Succeeded)
        {
            return Problem(
                statusCode: result.StatusCode,
                title: result.Code,
                detail: result.Message);
        }

        return Ok(result.Value);
    }

    [HttpPost("{id:int}/submit")]
    [ProducesResponseType<ContractSubmissionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ContractSubmissionErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SubmitContract(
        int id,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await contractCommandService.SubmitContractAsync(
            id,
            idempotencyKey,
            cancellationToken);
        if (!result.Succeeded)
        {
            if (result.Code == "ContractDoesNotMeetCtr02")
            {
                return BadRequest(new ContractSubmissionErrorResponse(
                    result.Code,
                    result.Message!,
                    result.Errors!));
            }

            return Problem(
                statusCode: result.StatusCode,
                title: result.Code,
                detail: result.Message);
        }

        return Ok(result.Value);
    }
}
