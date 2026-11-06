namespace ErpOrders.Api.Domain;

public enum OrderStatus
{
    Draft,
    Confirmed,
    Invoiced,
    Cancelled,
}

/// <summary>
/// Sales order. Lifecycle: Draft → Confirmed (stock reserved) → Invoiced (NF-e issued).
/// Draft and Confirmed orders can be cancelled; cancelling a confirmed order returns the stock.
/// </summary>
public class Order
{
    private readonly List<OrderItem> _items = [];

    public int Id { get; private set; }
    public int CustomerId { get; private set; }
    public Customer Customer { get; private set; } = null!;
    public OrderStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public Invoice? Invoice { get; private set; }
    public IReadOnlyCollection<OrderItem> Items => _items;

    public decimal Total => _items.Sum(i => i.LineTotal);

    private Order() { } // EF Core

    public Order(Customer customer, DateTime createdAt)
    {
        Customer = customer ?? throw new ArgumentNullException(nameof(customer));
        CustomerId = customer.Id;
        CreatedAt = createdAt;
        Status = OrderStatus.Draft;
    }

    /// <summary>Adds a product; adding the same product twice increases its quantity.</summary>
    public void AddItem(Product product, int quantity)
    {
        EnsureStatus(OrderStatus.Draft, "add items to");
        if (quantity <= 0)
            throw new DomainException("Quantity must be greater than zero.");

        var existing = _items.FirstOrDefault(i => i.Product == product);
        if (existing is not null)
            existing.Increase(quantity);
        else
            _items.Add(new OrderItem(product, quantity));
    }

    /// <summary>Reserves stock for every item. All-or-nothing: nothing changes if any item lacks stock.</summary>
    public void Confirm()
    {
        EnsureStatus(OrderStatus.Draft, "confirm");
        if (_items.Count == 0)
            throw new DomainException("An order needs at least one item to be confirmed.");

        var shortage = _items.FirstOrDefault(i => i.Quantity > i.Product.StockQuantity);
        if (shortage is not null)
            throw new DomainException(
                $"Insufficient stock for {shortage.Product.Sku}: requested {shortage.Quantity}, " +
                $"available {shortage.Product.StockQuantity}.");

        foreach (var item in _items)
            item.Product.RemoveStock(item.Quantity);

        Status = OrderStatus.Confirmed;
    }

    public void Cancel()
    {
        if (Status is OrderStatus.Invoiced)
            throw new DomainException("An invoiced order cannot be cancelled.");
        if (Status is OrderStatus.Cancelled)
            throw new DomainException("Order is already cancelled.");

        if (Status is OrderStatus.Confirmed)
            foreach (var item in _items)
                item.Product.AddStock(item.Quantity);

        Status = OrderStatus.Cancelled;
    }

    internal void MarkInvoiced(Invoice invoice)
    {
        EnsureStatus(OrderStatus.Confirmed, "invoice");
        Invoice = invoice;
        Status = OrderStatus.Invoiced;
    }

    private void EnsureStatus(OrderStatus expected, string action)
    {
        if (Status != expected)
            throw new DomainException($"Cannot {action} an order with status {Status}; expected {expected}.");
    }
}

public class OrderItem
{
    public int Id { get; private set; }
    public int OrderId { get; private set; }
    public int ProductId { get; private set; }
    public Product Product { get; private set; } = null!;
    public int Quantity { get; private set; }

    /// <summary>Price snapshot taken when the item was added, so later price changes don't affect the order.</summary>
    public decimal UnitPrice { get; private set; }

    public decimal LineTotal => Quantity * UnitPrice;

    private OrderItem() { } // EF Core

    internal OrderItem(Product product, int quantity)
    {
        Product = product;
        ProductId = product.Id;
        Quantity = quantity;
        UnitPrice = product.UnitPrice;
    }

    internal void Increase(int quantity) => Quantity += quantity;
}
