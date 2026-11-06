using ErpOrders.Api.Domain;

namespace ErpOrders.Api.Contracts;

public static class Mappings
{
    public static CustomerResponse ToResponse(this Customer c) =>
        new(c.Id, c.Name, c.Document, c.Email, c.State);

    public static ProductResponse ToResponse(this Product p) =>
        new(p.Id, p.Sku, p.Name, p.UnitPrice, p.StockQuantity);

    public static OrderResponse ToResponse(this Order o) =>
        new(
            o.Id,
            o.CustomerId,
            o.Customer.Name,
            o.Status,
            o.CreatedAt,
            o.Total,
            o.Items
                .Select(i => new OrderItemResponse(i.ProductId, i.Product.Sku, i.Product.Name, i.Quantity, i.UnitPrice, i.LineTotal))
                .ToList(),
            o.Invoice?.Id);

    public static InvoiceResponse ToResponse(this Invoice i) =>
        new(
            i.Id,
            i.Series,
            i.Number,
            i.AccessKey,
            i.OrderId,
            i.Order.Customer.Name,
            i.Order.Customer.Document,
            i.IssuedAt,
            i.TotalAmount,
            i.TaxRate,
            i.TaxAmount);
}
