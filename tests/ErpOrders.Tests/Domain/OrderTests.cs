using ErpOrders.Api.Domain;

namespace ErpOrders.Tests.Domain;

public class OrderTests
{
    private static readonly DateTime Now = new(2026, 11, 6, 12, 0, 0, DateTimeKind.Utc);

    private static Customer NewCustomer() => new("ACME Agro", "11.222.333/0001-81", "buyer@acme.com", "PR");

    [Fact]
    public void Total_is_the_sum_of_quantity_times_unit_price()
    {
        var order = new Order(NewCustomer(), Now);
        order.AddItem(new Product("A", "Seed", 10.50m, 100), 3);
        order.AddItem(new Product("B", "Fertilizer", 99.90m, 100), 2);

        Assert.Equal(231.30m, order.Total);
    }

    [Fact]
    public void Adding_the_same_product_twice_merges_the_line()
    {
        var product = new Product("A", "Seed", 10m, 100);
        var order = new Order(NewCustomer(), Now);
        order.AddItem(product, 2);
        order.AddItem(product, 5);

        var item = Assert.Single(order.Items);
        Assert.Equal(7, item.Quantity);
    }

    [Fact]
    public void Item_keeps_the_price_from_when_it_was_added()
    {
        var order = new Order(NewCustomer(), Now);
        order.AddItem(new Product("A", "Seed", 10m, 100), 1);

        Assert.Equal(10m, order.Items.Single().UnitPrice);
    }

    [Fact]
    public void Confirm_reserves_stock()
    {
        var product = new Product("A", "Seed", 10m, 10);
        var order = new Order(NewCustomer(), Now);
        order.AddItem(product, 4);

        order.Confirm();

        Assert.Equal(OrderStatus.Confirmed, order.Status);
        Assert.Equal(6, product.StockQuantity);
    }

    [Fact]
    public void Confirm_is_all_or_nothing_when_any_item_lacks_stock()
    {
        var enough = new Product("A", "Seed", 10m, 10);
        var notEnough = new Product("B", "Fertilizer", 10m, 1);
        var order = new Order(NewCustomer(), Now);
        order.AddItem(enough, 5);
        order.AddItem(notEnough, 2);

        var ex = Assert.Throws<DomainException>(order.Confirm);

        Assert.Contains("Insufficient stock for B", ex.Message);
        Assert.Equal(10, enough.StockQuantity); // untouched
        Assert.Equal(OrderStatus.Draft, order.Status);
    }

    [Fact]
    public void Cannot_confirm_an_empty_order() =>
        Assert.Throws<DomainException>(new Order(NewCustomer(), Now).Confirm);

    [Fact]
    public void Cancelling_a_confirmed_order_returns_stock()
    {
        var product = new Product("A", "Seed", 10m, 10);
        var order = new Order(NewCustomer(), Now);
        order.AddItem(product, 4);
        order.Confirm();

        order.Cancel();

        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Equal(10, product.StockQuantity);
    }

    [Fact]
    public void Cannot_add_items_after_confirmation()
    {
        var product = new Product("A", "Seed", 10m, 10);
        var order = new Order(NewCustomer(), Now);
        order.AddItem(product, 1);
        order.Confirm();

        Assert.Throws<DomainException>(() => order.AddItem(product, 1));
    }

    [Fact]
    public void Issuing_an_invoice_computes_tax_and_locks_the_order()
    {
        var order = new Order(NewCustomer(), Now);
        order.AddItem(new Product("A", "Seed", 33.33m, 10), 3); // 99.99
        order.Confirm();

        var invoice = Invoice.Issue(order, series: 1, number: 7, accessKey: new string('1', 44), Now, taxRate: 0.18m);

        Assert.Equal(99.99m, invoice.TotalAmount);
        Assert.Equal(18.00m, invoice.TaxAmount); // 17.9982 rounded
        Assert.Equal(OrderStatus.Invoiced, order.Status);
        Assert.Throws<DomainException>(order.Cancel);
    }

    [Fact]
    public void Cannot_invoice_a_draft_order()
    {
        var order = new Order(NewCustomer(), Now);
        order.AddItem(new Product("A", "Seed", 10m, 10), 1);

        Assert.Throws<DomainException>(() => Invoice.Issue(order, 1, 1, new string('1', 44), Now, 0.18m));
    }
}
