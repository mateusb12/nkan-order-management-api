using Microsoft.EntityFrameworkCore;
using OrderManagement.Shared.Models;

namespace OrderManagement.Shared.Data;

public class Database : DbContext
{
    public Database(DbContextOptions<Database> options)
        : base(options)
    {
    }

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>()
            .HasMany(o => o.Items)
            .WithOne(i => i.Order)
            .HasForeignKey(i => i.OrderId);


        modelBuilder.Entity<Order>()
            .Property(o => o.TotalAmount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<OrderItem>()
            .Property(i => i.UnitPrice)
            .HasPrecision(18, 2);

        base.OnModelCreating(modelBuilder);
    }
}
