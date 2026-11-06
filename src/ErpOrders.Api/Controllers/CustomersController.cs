using ErpOrders.Api.Contracts;
using ErpOrders.Api.Data;
using ErpOrders.Api.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ErpOrders.Api.Controllers;

[ApiController]
[Route("api/customers")]
[Produces("application/json")]
public class CustomersController(AppDbContext db) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<CustomerResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<CustomerResponse>> Create(CreateCustomerRequest request, CancellationToken ct)
    {
        var customer = new Customer(request.Name, request.Document, request.Email, request.State);

        if (await db.Customers.AnyAsync(c => c.Document == customer.Document, ct))
            throw new ConflictException($"A customer with document {customer.Document} already exists.");

        db.Customers.Add(customer);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = customer.Id }, customer.ToResponse());
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<CustomerResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerResponse>> GetById(int id, CancellationToken ct)
    {
        var customer = await db.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new NotFoundException($"Customer {id} not found.");
        return customer.ToResponse();
    }

    [HttpGet]
    public async Task<PagedResponse<CustomerResponse>> List(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        (page, pageSize) = Paging.Normalize(page, pageSize);
        var total = await db.Customers.CountAsync(ct);
        var items = await db.Customers.AsNoTracking()
            .OrderBy(c => c.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResponse<CustomerResponse>(items.Select(c => c.ToResponse()).ToList(), page, pageSize, total);
    }
}
