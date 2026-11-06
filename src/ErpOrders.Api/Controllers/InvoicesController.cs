using ErpOrders.Api.Contracts;
using ErpOrders.Api.Data;
using ErpOrders.Api.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ErpOrders.Api.Controllers;

[ApiController]
[Route("api/invoices")]
[Produces("application/json")]
public class InvoicesController(AppDbContext db) : ControllerBase
{
    [HttpGet("{id:int}")]
    [ProducesResponseType<InvoiceResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InvoiceResponse>> GetById(int id, CancellationToken ct)
    {
        var invoice = await Query().FirstOrDefaultAsync(i => i.Id == id, ct)
            ?? throw new NotFoundException($"Invoice {id} not found.");
        return invoice.ToResponse();
    }

    /// <summary>Looks up an invoice by its 44-digit NF-e access key.</summary>
    [HttpGet("by-key/{accessKey}")]
    [ProducesResponseType<InvoiceResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<InvoiceResponse>> GetByAccessKey(string accessKey, CancellationToken ct)
    {
        if (!NfeAccessKey.IsValid(accessKey))
            throw new DomainException("Access key must have 44 digits and a valid check digit.");

        var invoice = await Query().FirstOrDefaultAsync(i => i.AccessKey == accessKey, ct)
            ?? throw new NotFoundException($"Invoice with access key {accessKey} not found.");
        return invoice.ToResponse();
    }

    [HttpGet]
    public async Task<PagedResponse<InvoiceResponse>> List(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        (page, pageSize) = Paging.Normalize(page, pageSize);
        var total = await db.Invoices.CountAsync(ct);
        var items = await Query()
            .OrderByDescending(i => i.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResponse<InvoiceResponse>(items.Select(i => i.ToResponse()).ToList(), page, pageSize, total);
    }

    private IQueryable<Invoice> Query() =>
        db.Invoices.AsNoTracking().Include(i => i.Order).ThenInclude(o => o.Customer);
}
