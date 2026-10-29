using Microsoft.AspNetCore.Mvc;
using QTKD.Api.Application;

namespace QTKD.Api.Controllers;

[ApiController]
[Route("api/customers")]
public sealed class CustomersController(ICustomerReadService customerReadService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CustomerReadModel>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CustomerReadModel>>> GetCustomers(
        CancellationToken cancellationToken)
    {
        var customers = await customerReadService.GetCustomersAsync(cancellationToken);
        return Ok(customers);
    }
}
