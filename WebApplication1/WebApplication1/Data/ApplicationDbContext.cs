using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductSpecification> ProductSpecifications => Set<ProductSpecification>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Product>()
            .Property(p => p.Price)
            .HasPrecision(18, 2);

        builder.Entity<Order>()
            .Property(o => o.Subtotal)
            .HasPrecision(18, 2);
        builder.Entity<Order>()
            .Property(o => o.ShippingFee)
            .HasPrecision(18, 2);
        builder.Entity<Order>()
            .Property(o => o.Total)
            .HasPrecision(18, 2);

        builder.Entity<OrderItem>()
            .Property(oi => oi.UnitPrice)
            .HasPrecision(18, 2);

        builder.Entity<CartItem>()
            .HasIndex(c => new { c.UserId, c.ProductId })
            .IsUnique()
            .HasFilter("[UserId] IS NOT NULL");

        builder.Entity<CartItem>()
            .HasIndex(c => new { c.SessionId, c.ProductId })
            .IsUnique()
            .HasFilter("[SessionId] IS NOT NULL");

        builder.Entity<ProductSpecification>()
            .HasIndex(s => new { s.Key, s.Value });

        builder.Entity<Category>()
            .HasOne(c => c.ParentCategory)
            .WithMany(c => c.SubCategories)
            .HasForeignKey(c => c.ParentCategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
