using System.ComponentModel.DataAnnotations;
using ErpOrders.Api.Domain;

namespace ErpOrders.Api.Contracts;

public record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

// Customers
public record CreateCustomerRequest(
    [Required, StringLength(150)] string Name,
    [Required, StringLength(18)] string Document,
    [Required, EmailAddress, StringLength(254)] string Email,
    [Required, StringLength(2, MinimumLength = 2)] string State);

public record CustomerResponse(int Id, string Name, string Document, string Email, string State);

// Products
public record CreateProductRequest(
    [Required, StringLength(30)] string Sku,
    [Required, StringLength(150)] string Name,
    [Range(0.01, 1_000_000)] decimal UnitPrice,
    [Range(0, int.MaxValue)] int StockQuantity);

public record AddStockRequest([Range(1, 1_000_000)] int Quantity);

public record ProductResponse(int Id, string Sku, string Name, decimal UnitPrice, int StockQuantity);

// Orders
public record CreateOrderRequest(
    [Range(1, int.MaxValue)] int CustomerId,
    [Required, MinLength(1)] List<CreateOrderItemRequest> Items);

public record CreateOrderItemRequest(
    [Range(1, int.MaxValue)] int ProductId,
    [Range(1, 10_000)] int Quantity);

public record OrderItemResponse(int ProductId, string Sku, string ProductName, int Quantity, decimal UnitPrice, decimal LineTotal);

public record OrderResponse(
    int Id,
    int CustomerId,
    string CustomerName,
    OrderStatus Status,
    DateTime CreatedAt,
    decimal Total,
    IReadOnlyList<OrderItemResponse> Items,
    int? InvoiceId);

// Invoices
public record InvoiceResponse(
    int Id,
    int Series,
    int Number,
    string AccessKey,
    int OrderId,
    string CustomerName,
    string CustomerDocument,
    DateTime IssuedAt,
    decimal TotalAmount,
    decimal TaxRate,
    decimal TaxAmount);
