using ErpOrders.Api.Contracts;
using ErpOrders.Api.Domain;
using ErpOrders.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace ErpOrders.Api.Controllers;

[ApiController]
[Route("api/orders")]
[Produces("application/json")]
public class OrdersController(OrderService orders) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderResponse>> Create(CreateOrderRequest request, CancellationToken ct)
    {
        var order = await orders.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = order.Id }, order.ToResponse());
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderResponse>> GetById(int id, CancellationToken ct) =>
        (await orders.GetAsync(id, ct)).ToResponse();

    [HttpGet]
    public Task<PagedResponse<OrderResponse>> List(
        [FromQuery] OrderStatus? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        (page, pageSize) = Paging.Normalize(page, pageSize);
        return orders.ListAsync(status, page, pageSize, ct);
    }

    /// <summary>Confirms a draft order and reserves stock for all items.</summary>
    [HttpPost("{id:int}/confirm")]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<OrderResponse>> Confirm(int id, CancellationToken ct) =>
        (await orders.ConfirmAsync(id, ct)).ToResponse();

    /// <summary>Cancels a draft or confirmed order, returning reserved stock.</summary>
    [HttpPost("{id:int}/cancel")]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<OrderResponse>> Cancel(int id, CancellationToken ct) =>
        (await orders.CancelAsync(id, ct)).ToResponse();

    /// <summary>Issues the NF-e for a confirmed order.</summary>
    [HttpPost("{id:int}/invoice")]
    [ProducesResponseType<InvoiceResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<InvoiceResponse>> Invoice(int id, CancellationToken ct)
    {
        var invoice = await orders.IssueInvoiceAsync(id, ct);
        return CreatedAtAction(nameof(InvoicesController.GetById), "Invoices", new { id = invoice.Id }, invoice.ToResponse());
    }
}
