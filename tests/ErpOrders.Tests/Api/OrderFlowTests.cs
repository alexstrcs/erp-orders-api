using System.Net;
using ErpOrders.Api.Contracts;
using ErpOrders.Api.Domain;
using Microsoft.AspNetCore.Mvc;

namespace ErpOrders.Tests.Api;

public class OrderFlowTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Full_flow_create_confirm_and_invoice_an_order()
    {
        var customer = await CreateCustomer();
        var seed = await CreateProduct(unitPrice: 25.00m, stock: 50);
        var fertilizer = await CreateProduct(unitPrice: 120.00m, stock: 10);

        var createResponse = await _client.PostJsonAsync("/api/orders", new CreateOrderRequest(customer.Id,
        [
            new CreateOrderItemRequest(seed.Id, 4),        // 100.00
            new CreateOrderItemRequest(fertilizer.Id, 2),  // 240.00
        ]));
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var order = await createResponse.ReadAsync<OrderResponse>();
        Assert.Equal(OrderStatus.Draft, order.Status);
        Assert.Equal(340.00m, order.Total);

        var confirmResponse = await _client.PostAsync($"/api/orders/{order.Id}/confirm", null);
        Assert.Equal(HttpStatusCode.OK, confirmResponse.StatusCode);
        Assert.Equal(46, (await GetProduct(seed.Id)).StockQuantity);
        Assert.Equal(8, (await GetProduct(fertilizer.Id)).StockQuantity);

        var invoiceResponse = await _client.PostAsync($"/api/orders/{order.Id}/invoice", null);
        Assert.Equal(HttpStatusCode.Created, invoiceResponse.StatusCode);
        var invoice = await invoiceResponse.ReadAsync<InvoiceResponse>();
        Assert.Equal(340.00m, invoice.TotalAmount);
        Assert.Equal(61.20m, invoice.TaxAmount);
        Assert.True(NfeAccessKey.IsValid(invoice.AccessKey));
        Assert.Equal(customer.Document, invoice.CustomerDocument);

        var byKey = await _client.GetAsync($"/api/invoices/by-key/{invoice.AccessKey}");
        Assert.Equal(invoice.Id, (await byKey.ReadAsync<InvoiceResponse>()).Id);

        var finalOrder = await (await _client.GetAsync($"/api/orders/{order.Id}")).ReadAsync<OrderResponse>();
        Assert.Equal(OrderStatus.Invoiced, finalOrder.Status);
        Assert.Equal(invoice.Id, finalOrder.InvoiceId);
    }

    [Fact]
    public async Task Invoice_numbers_are_sequential()
    {
        var first = await IssueInvoiceForNewOrder();
        var second = await IssueInvoiceForNewOrder();

        Assert.Equal(first.Number + 1, second.Number);
        Assert.NotEqual(first.AccessKey, second.AccessKey);
    }

    [Fact]
    public async Task Cancelling_a_confirmed_order_returns_stock()
    {
        var customer = await CreateCustomer();
        var product = await CreateProduct(unitPrice: 10m, stock: 5);
        var order = await CreateOrder(customer.Id, product.Id, quantity: 5);
        await _client.PostAsync($"/api/orders/{order.Id}/confirm", null);
        Assert.Equal(0, (await GetProduct(product.Id)).StockQuantity);

        var cancel = await _client.PostAsync($"/api/orders/{order.Id}/cancel", null);

        Assert.Equal(HttpStatusCode.OK, cancel.StatusCode);
        Assert.Equal(5, (await GetProduct(product.Id)).StockQuantity);
    }

    [Fact]
    public async Task Confirming_without_enough_stock_returns_422()
    {
        var customer = await CreateCustomer();
        var product = await CreateProduct(unitPrice: 10m, stock: 1);
        var order = await CreateOrder(customer.Id, product.Id, quantity: 3);

        var response = await _client.PostAsync($"/api/orders/{order.Id}/confirm", null);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var problem = await response.ReadAsync<ProblemDetails>();
        Assert.Contains("Insufficient stock", problem.Detail);
        Assert.Equal(1, (await GetProduct(product.Id)).StockQuantity);
    }

    [Fact]
    public async Task Invoicing_a_draft_order_returns_422()
    {
        var customer = await CreateCustomer();
        var product = await CreateProduct(unitPrice: 10m, stock: 10);
        var order = await CreateOrder(customer.Id, product.Id, quantity: 1);

        var response = await _client.PostAsync($"/api/orders/{order.Id}/invoice", null);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Order_for_unknown_customer_returns_404()
    {
        var product = await CreateProduct(unitPrice: 10m, stock: 10);

        var response = await _client.PostJsonAsync("/api/orders",
            new CreateOrderRequest(999_999, [new CreateOrderItemRequest(product.Id, 1)]));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Order_without_items_returns_400()
    {
        var customer = await CreateCustomer();

        var response = await _client.PostJsonAsync("/api/orders", new CreateOrderRequest(customer.Id, []));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Customer_with_invalid_cpf_returns_422()
    {
        var response = await _client.PostJsonAsync("/api/customers",
            new CreateCustomerRequest("John", "529.982.247-26", "john@example.com", "PR"));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Duplicate_sku_returns_409()
    {
        var sku = TestData.NewSku();
        await _client.PostJsonAsync("/api/products", new CreateProductRequest(sku, "Seed", 10m, 1));

        var response = await _client.PostJsonAsync("/api/products", new CreateProductRequest(sku, "Seed again", 10m, 1));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Orders_can_be_filtered_by_status()
    {
        var customer = await CreateCustomer();
        var product = await CreateProduct(unitPrice: 10m, stock: 10);
        var order = await CreateOrder(customer.Id, product.Id, quantity: 1);
        await _client.PostAsync($"/api/orders/{order.Id}/cancel", null);

        var page = await (await _client.GetAsync("/api/orders?status=Cancelled&pageSize=100"))
            .ReadAsync<PagedResponse<OrderResponse>>();

        Assert.Contains(page.Items, o => o.Id == order.Id);
        Assert.All(page.Items, o => Assert.Equal(OrderStatus.Cancelled, o.Status));
    }

    private async Task<InvoiceResponse> IssueInvoiceForNewOrder()
    {
        var customer = await CreateCustomer();
        var product = await CreateProduct(unitPrice: 10m, stock: 10);
        var order = await CreateOrder(customer.Id, product.Id, quantity: 1);
        await _client.PostAsync($"/api/orders/{order.Id}/confirm", null);
        return await (await _client.PostAsync($"/api/orders/{order.Id}/invoice", null)).ReadAsync<InvoiceResponse>();
    }

    private async Task<CustomerResponse> CreateCustomer()
    {
        var response = await _client.PostJsonAsync("/api/customers",
            new CreateCustomerRequest("Fazenda Boa Vista", TestData.NewCpf(), "compras@boavista.com.br", "PR"));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.ReadAsync<CustomerResponse>();
    }

    private async Task<ProductResponse> CreateProduct(decimal unitPrice, int stock)
    {
        var response = await _client.PostJsonAsync("/api/products",
            new CreateProductRequest(TestData.NewSku(), "Soybean seed 40kg", unitPrice, stock));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.ReadAsync<ProductResponse>();
    }

    private async Task<OrderResponse> CreateOrder(int customerId, int productId, int quantity)
    {
        var response = await _client.PostJsonAsync("/api/orders",
            new CreateOrderRequest(customerId, [new CreateOrderItemRequest(productId, quantity)]));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.ReadAsync<OrderResponse>();
    }

    private async Task<ProductResponse> GetProduct(int id) =>
        await (await _client.GetAsync($"/api/products/{id}")).ReadAsync<ProductResponse>();
}
