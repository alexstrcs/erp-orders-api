namespace ErpOrders.Api.Domain;

public class Product
{
    public int Id { get; private set; }
    public string Sku { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public decimal UnitPrice { get; private set; }
    public int StockQuantity { get; private set; }

    private Product() { } // EF Core

    public Product(string sku, string name, decimal unitPrice, int stockQuantity)
    {
        if (string.IsNullOrWhiteSpace(sku))
            throw new DomainException("SKU is required.");
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Product name is required.");
        if (unitPrice <= 0)
            throw new DomainException("Unit price must be greater than zero.");
        if (stockQuantity < 0)
            throw new DomainException("Stock quantity cannot be negative.");

        Sku = sku.Trim().ToUpperInvariant();
        Name = name.Trim();
        UnitPrice = decimal.Round(unitPrice, 2);
        StockQuantity = stockQuantity;
    }

    public void AddStock(int quantity)
    {
        if (quantity <= 0)
            throw new DomainException("Quantity must be greater than zero.");
        StockQuantity += quantity;
    }

    public void RemoveStock(int quantity)
    {
        if (quantity <= 0)
            throw new DomainException("Quantity must be greater than zero.");
        if (quantity > StockQuantity)
            throw new DomainException(
                $"Insufficient stock for {Sku}: requested {quantity}, available {StockQuantity}.");
        StockQuantity -= quantity;
    }
}
