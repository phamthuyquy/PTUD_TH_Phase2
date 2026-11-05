using Microsoft.AspNetCore.Mvc;
using QTKD.Api.Application;

namespace QTKD.Api.Controllers;

[ApiController]
[Route("api/customers")]
public sealed class CustomersController(ICustomerService customerService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CustomerReadModel>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CustomerReadModel>>> GetCustomers(
        CancellationToken cancellationToken)
    {
        var customers = await customerService.GetCustomersAsync(cancellationToken);
        return Ok(customers);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<CustomerResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerResponse>> GetCustomerById(
        int id,
        CancellationToken cancellationToken)
    {
        var customer = await customerService.GetCustomerByIdAsync(id, cancellationToken);
        return customer is null ? NotFound() : Ok(customer);
    }

    [HttpPost]
    [ProducesResponseType<CustomerResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateCustomer(
        CreateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var result = await customerService.CreateCustomerAsync(request, cancellationToken);
        if (!result.Succeeded)
        {
            return Problem(
                statusCode: result.StatusCode,
                title: result.Code,
                detail: result.Message);
        }

        return CreatedAtAction(
            nameof(GetCustomerById),
            new { id = result.Value!.Id },
            result.Value);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType<CustomerResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateCustomer(
        int id,
        UpdateCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var result = await customerService.UpdateCustomerAsync(id, request, cancellationToken);
        if (!result.Succeeded)
        {
            return Problem(
                statusCode: result.StatusCode,
                title: result.Code,
                detail: result.Message);
        }

        return Ok(result.Value);
    }
}
