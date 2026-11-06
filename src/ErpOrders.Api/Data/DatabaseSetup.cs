using Microsoft.EntityFrameworkCore;

namespace ErpOrders.Api.Data;

public static class DatabaseSetup
{
    /// <summary>
    /// Registers <see cref="AppDbContext"/> for the provider set in "Database:Provider":
    /// "SqlServer" for production-like environments, "Sqlite" (default) for local runs and tests.
    /// </summary>
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration["Database:Provider"] ?? "Sqlite";
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' is not configured.");

        services.AddDbContext<AppDbContext>(options =>
        {
            switch (provider.ToLowerInvariant())
            {
                case "sqlserver":
                    options.UseSqlServer(connectionString);
                    break;
                case "sqlite":
                    options.UseSqlite(connectionString);
                    break;
                default:
                    throw new InvalidOperationException(
                        $"Unsupported database provider '{provider}'. Use 'SqlServer' or 'Sqlite'.");
            }
        });

        return services;
    }

    /// <summary>Creates the schema on startup when "Database:EnsureCreated" is true (local/demo use).</summary>
    public static void EnsureDatabaseCreated(this WebApplication app)
    {
        if (!app.Configuration.GetValue<bool>("Database:EnsureCreated")) return;

        using var scope = app.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
    }
}
