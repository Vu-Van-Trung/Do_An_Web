using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        await context.Database.MigrateAsync();

        await SeedRolesAsync(roleManager);
        await SeedAdminAsync(userManager);
        if (await context.Categories.AnyAsync())
            return;

        var categories = new[]
        {
            new Category { Name = "Keyboards", Slug = "keyboards", Description = "Mechanical and membrane gaming keyboards" },
            new Category { Name = "Mice", Slug = "mice", Description = "Precision gaming mice" },
            new Category { Name = "Headsets", Slug = "headsets", Description = "Immersive audio gear" },
            new Category { Name = "Chairs", Slug = "chairs", Description = "Ergonomic gaming chairs" }
        };
        context.Categories.AddRange(categories);

        var brands = new[]
        {
            new Brand { Name = "Razer" },
            new Brand { Name = "Logitech" },
            new Brand { Name = "Corsair" },
            new Brand { Name = "Keychron" }
        };
        context.Brands.AddRange(brands);
        await context.SaveChangesAsync();

        var products = new List<Product>
        {
            CreateProduct("Razer DeathAdder V3 Pro", "Wireless esports mouse with Focus Pro 30K sensor.", 2890000, 25,
                categories[1].Id, brands[0].Id, "/images/products/placeholder.svg",
                ("DPI", "30000"), ("Connection", "Wireless"), ("Weight", "63g")),
            CreateProduct("Logitech G Pro X Superlight 2", "Ultra-light wireless mouse for competitive play.", 3490000, 18,
                categories[1].Id, brands[1].Id, "/images/products/placeholder.svg",
                ("DPI", "32000"), ("Connection", "Wireless"), ("Weight", "60g")),
            CreateProduct("Corsair K70 RGB PRO", "Mechanical gaming keyboard with Cherry MX switches.", 4290000, 12,
                categories[0].Id, brands[2].Id, "/images/products/placeholder.svg",
                ("Switch Type", "Cherry MX Red"), ("Connection", "Wired"), ("Layout", "Full-size")),
            CreateProduct("Keychron Q1 Pro", "Customizable wireless mechanical keyboard.", 3990000, 20,
                categories[0].Id, brands[3].Id, "/images/products/placeholder.svg",
                ("Switch Type", "Gateron Pro Brown"), ("Connection", "Wireless"), ("Layout", "75%")),
            CreateProduct("Razer BlackShark V2 Pro", "THX-certified wireless gaming headset.", 5490000, 15,
                categories[2].Id, brands[0].Id, "/images/products/placeholder.svg",
                ("Connection", "Wireless"), ("Driver", "50mm"), ("Mic", "Detachable")),
            CreateProduct("Logitech G733 Lightspeed", "Lightweight RGB wireless headset.", 2990000, 22,
                categories[2].Id, brands[1].Id, "/images/products/placeholder.svg",
                ("Connection", "Wireless"), ("Driver", "40mm"), ("Weight", "278g")),
            CreateProduct("Corsair T3 Rush", "Fabric gaming chair with 4D armrests.", 8990000, 8,
                categories[3].Id, brands[2].Id, "/images/products/placeholder.svg",
                ("Material", "Fabric"), ("Max Load", "120kg"), ("Recline", "160°")),
            CreateProduct("Razer Huntsman V3 Pro", "Analog optical gaming keyboard.", 5990000, 10,
                categories[0].Id, brands[0].Id, "/images/products/placeholder.svg",
                ("Switch Type", "Analog Optical"), ("Connection", "Wired"), ("Layout", "Full-size"))
        };

        context.Products.AddRange(products);
        await context.SaveChangesAsync();
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        foreach (var role in new[] { "Admin", "Customer" })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    private static async Task SeedAdminAsync(UserManager<ApplicationUser> userManager)
    {
        const string email = "admin@nexusgear.local";
        if (await userManager.FindByEmailAsync(email) != null)
            return;

        var admin = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = "Store Administrator"
        };
        await userManager.CreateAsync(admin, "Admin@123");
        await userManager.AddToRoleAsync(admin, "Admin");
    }

    private static Product CreateProduct(string name, string description, decimal price, int stock,
        int categoryId, int brandId, string imageUrl, params ValueTuple<string, string>[] specs)
    {
        var product = new Product
        {
            Name = name,
            Description = description,
            Price = price,
            Stock = stock,
            CategoryId = categoryId,
            BrandId = brandId,
            ImageUrl = imageUrl
        };
        foreach (var (key, value) in specs)
        {
            product.Specifications.Add(new ProductSpecification { Key = key, Value = value });
        }
        return product;
    }
}
