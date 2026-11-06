using System.Security.Cryptography;
using ErpOrders.Api.Contracts;
using ErpOrders.Api.Data;
using ErpOrders.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ErpOrders.Api.Services;

public class OrderService(AppDbContext db, TimeProvider clock, IOptions<InvoiceOptions> invoiceOptions)
{
    private readonly InvoiceOptions _invoice = invoiceOptions.Value;

    public async Task<Order> CreateAsync(CreateOrderRequest request, CancellationToken ct)
    {
        var customer = await db.Customers.FindAsync([request.CustomerId], ct)
            ?? throw new NotFoundException($"Customer {request.CustomerId} not found.");

        var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await db.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, ct);

        var missing = productIds.Where(id => !products.ContainsKey(id)).ToList();
        if (missing.Count > 0)
            throw new NotFoundException($"Product(s) not found: {string.Join(", ", missing)}.");

        var order = new Order(customer, clock.GetUtcNow().UtcDateTime);
        foreach (var item in request.Items)
            order.AddItem(products[item.ProductId], item.Quantity);

        db.Orders.Add(order);
        await db.SaveChangesAsync(ct);
        return order;
    }

    public async Task<Order> GetAsync(int id, CancellationToken ct) =>
        await LoadOrderAsync(id, ct);

    public async Task<PagedResponse<OrderResponse>> ListAsync(OrderStatus? status, int page, int pageSize, CancellationToken ct)
    {
        var query = db.Orders.AsNoTracking();
        if (status is not null)
            query = query.Where(o => o.Status == status);

        var total = await query.CountAsync(ct);
        var orders = await query
            .Include(o => o.Customer)
            .Include(o => o.Invoice)
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .OrderByDescending(o => o.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsSplitQuery()
            .ToListAsync(ct);

        return new PagedResponse<OrderResponse>(orders.Select(o => o.ToResponse()).ToList(), page, pageSize, total);
    }

    public async Task<Order> ConfirmAsync(int id, CancellationToken ct)
    {
        var order = await LoadOrderAsync(id, ct);
        order.Confirm();
        await db.SaveChangesAsync(ct);
        return order;
    }

    public async Task<Order> CancelAsync(int id, CancellationToken ct)
    {
        var order = await LoadOrderAsync(id, ct);
        order.Cancel();
        await db.SaveChangesAsync(ct);
        return order;
    }

    public async Task<Invoice> IssueInvoiceAsync(int id, CancellationToken ct)
    {
        var order = await LoadOrderAsync(id, ct);
        if (order.Status != OrderStatus.Confirmed)
            throw new DomainException($"Only confirmed orders can be invoiced; order {id} is {order.Status}.");

        var issuedAt = clock.GetUtcNow().UtcDateTime;
        var lastNumber = await db.Invoices
            .Where(i => i.Series == _invoice.Series)
            .MaxAsync(i => (int?)i.Number, ct) ?? 0;
        var number = lastNumber + 1;

        var accessKey = NfeAccessKey.Create(
            _invoice.IssuerStateCode,
            issuedAt,
            _invoice.IssuerCnpj,
            _invoice.Series,
            number,
            RandomNumberGenerator.GetInt32(0, 100_000_000));

        var invoice = Invoice.Issue(order, _invoice.Series, number, accessKey, issuedAt, _invoice.TaxRate);
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync(ct);
        return invoice;
    }

    private async Task<Order> LoadOrderAsync(int id, CancellationToken ct) =>
        await db.Orders
            .Include(o => o.Customer)
            .Include(o => o.Invoice)
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .AsSplitQuery()
            .FirstOrDefaultAsync(o => o.Id == id, ct)
        ?? throw new NotFoundException($"Order {id} not found.");
}
