# ERP Orders API

![CI](https://github.com/alexstrcs/erp-orders-api/actions/workflows/ci.yml/badge.svg)

A REST API for the core of an ERP sales flow — **customers, products, orders and Brazilian electronic invoices (NF-e)** — built with **C# / .NET 10, ASP.NET Core and Entity Framework Core**, running on **SQL Server** or SQLite.

It models the rules I worked with for years in agroindustrial ERPs: stock reservation on order confirmation, all-or-nothing stock checks, price snapshots on order items, sequential invoice numbering and the 44-digit NF-e access key with its mod-11 check digit.

## Features

- **Customers** with CPF/CNPJ validation (official check-digit algorithm) and Brazilian state (UF) validation
- **Products** with unique SKU, price and stock control (`POST /api/products/{id}/stock` to receive goods)
- **Orders** with a status lifecycle:

  ```
  Draft ──confirm──▶ Confirmed ──invoice──▶ Invoiced
    │                    │
    └──────cancel────────┴──▶ Cancelled   (cancelling a confirmed order returns its stock)
  ```

- **Invoices (simplified NF-e)**: sequential number per series, 44-digit access key, flat tax rate, lookup by access key
- **Problem Details (RFC 9457)** error responses: `400` validation, `404` not found, `409` duplicate, `422` business rule
- **Pagination** on every list endpoint, order filtering by status
- **OpenAPI** document + interactive docs with [Scalar](https://scalar.com) in Development
- **40 automated tests**: domain unit tests and end-to-end API tests with `WebApplicationFactory`
- **CI** with GitHub Actions on every push

## Tech stack

| | |
|---|---|
| Language / runtime | C# 14, .NET 10 |
| Web | ASP.NET Core (controllers), OpenAPI, Scalar |
| Data | Entity Framework Core 10 — SQL Server or SQLite |
| Tests | xUnit, `Microsoft.AspNetCore.Mvc.Testing` |
| CI | GitHub Actions |

## Getting started

Requirements: [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
git clone https://github.com/alexstrcs/erp-orders-api.git
cd erp-orders-api
dotnet run --project src/ErpOrders.Api
```

The API starts on `http://localhost:5231` using a local SQLite file, created automatically. Open **http://localhost:5231/scalar** for the interactive docs.

### Using SQL Server

Set the provider and connection string through environment variables (or `appsettings.json`):

```bash
# PowerShell
$env:Database__Provider = "SqlServer"
$env:ConnectionStrings__Default = "Server=(localdb)\MSSQLLocalDB;Database=ErpOrders;Trusted_Connection=True;TrustServerCertificate=True"
dotnet run --project src/ErpOrders.Api
```

### Running the tests

```bash
dotnet test
```

## Example: from order to invoice

```http
POST /api/customers
{ "name": "Fazenda Boa Vista", "document": "529.982.247-25", "email": "compras@boavista.com.br", "state": "PR" }

POST /api/products
{ "sku": "SOY-40KG", "name": "Soybean seed 40kg", "unitPrice": 250.00, "stockQuantity": 100 }

POST /api/orders
{ "customerId": 1, "items": [ { "productId": 1, "quantity": 4 } ] }

POST /api/orders/1/confirm     → status Confirmed, stock 100 → 96
POST /api/orders/1/invoice     → 201 Created
```

```json
{
  "id": 1,
  "series": 1,
  "number": 1,
  "accessKey": "41261111222333000181550010000000011248325984",
  "orderId": 1,
  "customerName": "Fazenda Boa Vista",
  "customerDocument": "52998224725",
  "issuedAt": "2026-11-06T03:43:37.9357011Z",
  "totalAmount": 1000.00,
  "taxRate": 0.18,
  "taxAmount": 180.00
}
```

More requests ready to run in [`src/ErpOrders.Api/ErpOrders.Api.http`](src/ErpOrders.Api/ErpOrders.Api.http) (VS Code REST Client / Visual Studio / Rider).

## Endpoints

| Method | Route | Description |
|---|---|---|
| `POST` | `/api/customers` | Create customer |
| `GET` | `/api/customers` · `/api/customers/{id}` | List (paged) / get |
| `POST` | `/api/products` | Create product |
| `GET` | `/api/products` · `/api/products/{id}` | List (paged) / get |
| `POST` | `/api/products/{id}/stock` | Receive stock |
| `POST` | `/api/orders` | Create draft order |
| `GET` | `/api/orders?status=&page=&pageSize=` · `/api/orders/{id}` | List / get |
| `POST` | `/api/orders/{id}/confirm` | Confirm and reserve stock |
| `POST` | `/api/orders/{id}/cancel` | Cancel (returns stock if confirmed) |
| `POST` | `/api/orders/{id}/invoice` | Issue NF-e |
| `GET` | `/api/invoices` · `/api/invoices/{id}` | List / get |
| `GET` | `/api/invoices/by-key/{accessKey}` | Get by NF-e access key |

## Project structure

```
src/ErpOrders.Api
├── Domain/          # Entities and business rules (no framework dependencies)
│   ├── Order.cs         # lifecycle, stock reservation, price snapshot
│   ├── Invoice.cs       # issuing and tax calculation
│   ├── NfeAccessKey.cs  # 44-digit key + mod-11 check digit
│   └── TaxDocument.cs   # CPF / CNPJ validation
├── Data/            # EF Core DbContext, mappings, provider selection
├── Services/        # Order workflow (create, confirm, cancel, invoice)
├── Controllers/     # HTTP endpoints
├── Contracts/       # Request/response DTOs
└── Infrastructure/  # Exception → Problem Details mapping
tests/ErpOrders.Tests
├── Domain/          # Unit tests for business rules
└── Api/             # End-to-end tests through HTTP
```

## Design decisions

- **Rich domain model** — rules live in the entities (`Order.Confirm()`, `Product.RemoveStock()`), so they are enforced no matter which endpoint changes the data, and are unit-testable without a database.
- **All-or-nothing confirmation** — every item's stock is checked before any stock is removed, so a failed confirmation never leaves partial reservations.
- **Price snapshot** — order items store the unit price at the time they were added; later price changes don't alter existing orders.
- **Provider-agnostic data layer** — the same model runs on SQL Server (production) and SQLite (local runs and tests), selected by configuration.
- **`TimeProvider` injection** — the clock is injectable, keeping date-dependent logic (invoice date, access key) testable.

## Limitations and roadmap

This is a portfolio project, not a fiscal product. The NF-e here is simplified:

- No XML generation, digital signature or transmission to SEFAZ
- Single flat tax rate instead of real ICMS/IPI/PIS/COFINS rules per product and state
- Numeric CNPJ only (alphanumeric CNPJ not yet supported)

Next steps: EF Core migrations, authentication (JWT), optimistic concurrency on stock, Docker Compose with SQL Server.

## License

[MIT](LICENSE)
