using ErpOrders.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace ErpOrders.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Invoice> Invoices => Set<Invoice>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(e =>
        {
            e.Property(c => c.Name).HasMaxLength(150).IsRequired();
            e.Property(c => c.Document).HasMaxLength(14).IsRequired();
            e.Property(c => c.Email).HasMaxLength(254).IsRequired();
            e.Property(c => c.State).HasMaxLength(2).IsFixedLength().IsRequired();
            e.HasIndex(c => c.Document).IsUnique();
        });

        modelBuilder.Entity<Product>(e =>
        {
            e.Property(p => p.Sku).HasMaxLength(30).IsRequired();
            e.Property(p => p.Name).HasMaxLength(150).IsRequired();
            e.Property(p => p.UnitPrice).HasPrecision(18, 2);
            e.HasIndex(p => p.Sku).IsUnique();
        });

        modelBuilder.Entity<Order>(e =>
        {
            e.Property(o => o.Status).HasConversion<string>().HasMaxLength(20);
            e.Ignore(o => o.Total);
            e.HasOne(o => o.Customer).WithMany().HasForeignKey(o => o.CustomerId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(o => o.Items).WithOne().HasForeignKey(i => i.OrderId).OnDelete(DeleteBehavior.Cascade);
            e.Navigation(o => o.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
            e.HasIndex(o => o.Status);
        });

        modelBuilder.Entity<OrderItem>(e =>
        {
            e.Property(i => i.UnitPrice).HasPrecision(18, 2);
            e.Ignore(i => i.LineTotal);
            e.HasOne(i => i.Product).WithMany().HasForeignKey(i => i.ProductId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Invoice>(e =>
        {
            e.Property(i => i.AccessKey).HasMaxLength(44).IsFixedLength().IsRequired();
            e.Property(i => i.TotalAmount).HasPrecision(18, 2);
            e.Property(i => i.TaxAmount).HasPrecision(18, 2);
            e.Property(i => i.TaxRate).HasPrecision(5, 4);
            e.HasOne(i => i.Order).WithOne(o => o.Invoice).HasForeignKey<Invoice>(i => i.OrderId);
            e.HasIndex(i => new { i.Series, i.Number }).IsUnique();
            e.HasIndex(i => i.AccessKey).IsUnique();
        });
    }
}
