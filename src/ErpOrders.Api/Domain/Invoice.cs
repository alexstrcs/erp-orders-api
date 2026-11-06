namespace ErpOrders.Api.Domain;

/// <summary>
/// Simplified NF-e (Brazilian electronic invoice). It models numbering, the access key
/// and a single flat tax rate; it does not transmit anything to SEFAZ.
/// </summary>
public class Invoice
{
    public int Id { get; private set; }
    public int Series { get; private set; }
    public int Number { get; private set; }
    public string AccessKey { get; private set; } = string.Empty;
    public int OrderId { get; private set; }
    public Order Order { get; private set; } = null!;
    public DateTime IssuedAt { get; private set; }
    public decimal TotalAmount { get; private set; }
    public decimal TaxRate { get; private set; }
    public decimal TaxAmount { get; private set; }

    private Invoice() { } // EF Core

    private Invoice(Order order, int series, int number, string accessKey, DateTime issuedAt, decimal taxRate)
    {
        Order = order;
        OrderId = order.Id;
        Series = series;
        Number = number;
        AccessKey = accessKey;
        IssuedAt = issuedAt;
        TotalAmount = order.Total;
        TaxRate = taxRate;
        TaxAmount = decimal.Round(order.Total * taxRate, 2, MidpointRounding.AwayFromZero);
    }

    /// <summary>Issues the invoice and moves the order to <see cref="OrderStatus.Invoiced"/>.</summary>
    public static Invoice Issue(Order order, int series, int number, string accessKey, DateTime issuedAt, decimal taxRate)
    {
        if (taxRate is < 0 or > 1)
            throw new DomainException("Tax rate must be between 0 and 1.");

        var invoice = new Invoice(order, series, number, accessKey, issuedAt, taxRate);
        order.MarkInvoiced(invoice);
        return invoice;
    }
}
