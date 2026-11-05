using Microsoft.AspNetCore.Mvc;
using QTKD.Api.Application;

namespace QTKD.Api.Controllers;

[ApiController]
[Route("api/pricelists")]
public sealed class PriceListsController(IPricingService pricingService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<PriceListResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PriceListResponse>>> GetPriceLists(
        CancellationToken cancellationToken)
    {
        return Ok(await pricingService.GetPriceListsAsync(cancellationToken));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<PriceListResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PriceListResponse>> GetPriceList(
        int id,
        CancellationToken cancellationToken)
    {
        var priceList = await pricingService.GetPriceListByIdAsync(id, cancellationToken);
        return priceList is null ? NotFound() : Ok(priceList);
    }

    [HttpPost]
    [ProducesResponseType<PriceListResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreatePriceList(
        CreatePriceListRequest request,
        CancellationToken cancellationToken)
    {
        var result = await pricingService.CreatePriceListAsync(request, cancellationToken);
        if (!result.Succeeded)
        {
            return Problem(
                statusCode: result.StatusCode,
                title: result.Code,
                detail: result.Message);
        }

        return CreatedAtAction(
            nameof(GetPriceList),
            new { id = result.Value!.Id },
            result.Value);
    }

    [HttpGet("{id:int}/items")]
    [ProducesResponseType<IReadOnlyList<PriceItemResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<PriceItemResponse>>> GetPriceItems(
        int id,
        CancellationToken cancellationToken)
    {
        var items = await pricingService.GetPriceItemsAsync(id, cancellationToken);
        return items is null ? NotFound() : Ok(items);
    }

    [HttpGet("{id:int}/items/{serviceCode}")]
    [ProducesResponseType<PriceItemResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PriceItemResponse>> GetPriceItem(
        int id,
        string serviceCode,
        CancellationToken cancellationToken)
    {
        var item = await pricingService.GetPriceItemAsync(
            id,
            serviceCode,
            cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost("{id:int}/items")]
    [ProducesResponseType<PriceItemResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreatePriceItem(
        int id,
        CreatePriceItemRequest request,
        CancellationToken cancellationToken)
    {
        var result = await pricingService.CreatePriceItemAsync(id, request, cancellationToken);
        if (!result.Succeeded)
        {
            return Problem(
                statusCode: result.StatusCode,
                title: result.Code,
                detail: result.Message);
        }

        return CreatedAtAction(
            nameof(GetPriceItem),
            new { id, serviceCode = result.Value!.ServiceCode },
            result.Value);
    }
}
