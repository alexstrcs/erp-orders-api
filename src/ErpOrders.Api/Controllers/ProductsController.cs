using ErpOrders.Api.Contracts;
using ErpOrders.Api.Data;
using ErpOrders.Api.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ErpOrders.Api.Controllers;

[ApiController]
[Route("api/products")]
[Produces("application/json")]
public class ProductsController(AppDbContext db) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProductResponse>> Create(CreateProductRequest request, CancellationToken ct)
    {
        var product = new Product(request.Sku, request.Name, request.UnitPrice, request.StockQuantity);

        if (await db.Products.AnyAsync(p => p.Sku == product.Sku, ct))
            throw new ConflictException($"A product with SKU {product.Sku} already exists.");

        db.Products.Add(product);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product.ToResponse());
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductResponse>> GetById(int id, CancellationToken ct)
    {
        var product = await db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new NotFoundException($"Product {id} not found.");
        return product.ToResponse();
    }

    [HttpGet]
    public async Task<PagedResponse<ProductResponse>> List(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        (page, pageSize) = Paging.Normalize(page, pageSize);
        var total = await db.Products.CountAsync(ct);
        var items = await db.Products.AsNoTracking()
            .OrderBy(p => p.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResponse<ProductResponse>(items.Select(p => p.ToResponse()).ToList(), page, pageSize, total);
    }

    /// <summary>Receives goods into stock.</summary>
    [HttpPost("{id:int}/stock")]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductResponse>> AddStock(int id, AddStockRequest request, CancellationToken ct)
    {
        var product = await db.Products.FindAsync([id], ct)
            ?? throw new NotFoundException($"Product {id} not found.");

        product.AddStock(request.Quantity);
        await db.SaveChangesAsync(ct);
        return product.ToResponse();
    }
}
