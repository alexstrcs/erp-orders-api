using System.Text.Json.Serialization;
using ErpOrders.Api.Data;
using ErpOrders.Api.Infrastructure;
using ErpOrders.Api.Services;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.Configure<InvoiceOptions>(builder.Configuration.GetSection(InvoiceOptions.SectionName));
builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddScoped<OrderService>();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(); // interactive API docs at /scalar
}

app.EnsureDatabaseCreated();
app.MapControllers();

app.Run();

public partial class Program; // exposed for WebApplicationFactory in integration tests
