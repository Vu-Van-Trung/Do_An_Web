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

        // Seed initial discounts/vouchers if they don't exist
        if (!await context.Discounts.AnyAsync(d => d.Code == "NEWUSER200K"))
        {
            context.Discounts.Add(new Discount
            {
                Code = "NEWUSER200K",
                Name = "Quà tặng Đăng ký mới 200k",
                Description = "Giảm ngay 200.000đ cho đơn hàng từ 500.000đ trở lên",
                PromotionType = PromotionType.FirstOrder,
                DiscountType = DiscountType.Fixed,
                Value = 200000m,
                MinOrderAmount = 500000m,
                StartDate = DateTime.UtcNow.AddDays(-1),
                EndDate = DateTime.UtcNow.AddYears(10),
                IsActive = true
            });
        }

        if (!await context.Discounts.AnyAsync(d => d.Code == "NEWUSERFREE"))
        {
            context.Discounts.Add(new Discount
            {
                Code = "NEWUSERFREE",
                Name = "Miễn phí vận chuyển 0đ",
                Description = "Miễn phí vận chuyển cho khách hàng mới",
                PromotionType = PromotionType.FreeShipping,
                DiscountType = DiscountType.Fixed,
                Value = 0m,
                MinOrderAmount = 0m,
                StartDate = DateTime.UtcNow.AddDays(-1),
                EndDate = DateTime.UtcNow.AddYears(10),
                IsActive = true
            });
        }

        await context.SaveChangesAsync();

        // Skip if new hierarchical categories already exist
        if (await context.Categories.AnyAsync(c => c.Icon != null))
            return;

        // Clear old data for clean re-seed
        await ClearProductDataAsync(context);

        // Seed category hierarchy
        var (banPhim, chuot, taiNghe, gheGaming) = await SeedCategoriesAsync(context);

        // Seed brands
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
            CreateProduct("Razer DeathAdder V3 Pro", "Chuột gaming không dây với cảm biến Focus Pro 30K.", 2890000, 25,
                chuot.Id, brands[0].Id, "/images/products/placeholder.svg",
                ("DPI", "30000"), ("Connection", "Wireless"), ("Weight", "63g")),
            CreateProduct("Logitech G Pro X Superlight 2", "Chuột không dây siêu nhẹ dành cho esport.", 3490000, 18,
                chuot.Id, brands[1].Id, "/images/products/placeholder.svg",
                ("DPI", "32000"), ("Connection", "Wireless"), ("Weight", "60g")),
            CreateProduct("Corsair K70 RGB PRO", "Bàn phím cơ gaming với switch Cherry MX.", 4290000, 12,
                banPhim.Id, brands[2].Id, "/images/products/placeholder.svg",
                ("Switch Type", "Cherry MX Red"), ("Connection", "Wired"), ("Layout", "Full-size")),
            CreateProduct("Keychron Q1 Pro", "Bàn phím cơ không dây tùy biến cao.", 3990000, 20,
                banPhim.Id, brands[3].Id, "/images/products/placeholder.svg",
                ("Switch Type", "Gateron Pro Brown"), ("Connection", "Wireless"), ("Layout", "75%")),
            CreateProduct("Razer BlackShark V2 Pro", "Tai nghe gaming không dây chuẩn THX.", 5490000, 15,
                taiNghe.Id, brands[0].Id, "/images/products/placeholder.svg",
                ("Connection", "Wireless"), ("Driver", "50mm"), ("Mic", "Detachable")),
            CreateProduct("Logitech G733 Lightspeed", "Tai nghe không dây RGB nhẹ nhàng.", 2990000, 22,
                taiNghe.Id, brands[1].Id, "/images/products/placeholder.svg",
                ("Connection", "Wireless"), ("Driver", "40mm"), ("Weight", "278g")),
            CreateProduct("Corsair T3 Rush", "Ghế gaming vải cao cấp, tay ghế 4D.", 8990000, 8,
                gheGaming.Id, brands[2].Id, "/images/products/placeholder.svg",
                ("Material", "Fabric"), ("Max Load", "120kg"), ("Recline", "160°")),
            CreateProduct("Razer Huntsman V3 Pro", "Bàn phím gaming optical analog.", 5990000, 10,
                banPhim.Id, brands[0].Id, "/images/products/placeholder.svg",
                ("Switch Type", "Analog Optical"), ("Connection", "Wired"), ("Layout", "Full-size"))
        };

        context.Products.AddRange(products);
        await context.SaveChangesAsync();
    }

    private static async Task<(Category banPhim, Category chuot, Category taiNghe, Category gheGaming)>
        SeedCategoriesAsync(ApplicationDbContext context)
    {
        // ── Group 1: Thiết bị ngoại vi ──
        var ngoaiVi = new Category
        {
            Name = "Thiết bị ngoại vi",
            Slug = "thiet-bi-ngoai-vi",
            Description = "Bàn phím, chuột, tai nghe và phụ kiện gaming",
            Icon = "🎮",
            SortOrder = 1
        };
        var banPhim = new Category
        {
            Name = "Bàn phím gaming",
            Slug = "ban-phim-gaming",
            Description = "Bàn phím cơ Full-size, TKL, 60% và bàn phím giả cơ",
            Icon = "⌨️",
            SortOrder = 1,
            ParentCategory = ngoaiVi
        };
        var chuot = new Category
        {
            Name = "Chuột gaming",
            Slug = "chuot-gaming",
            Description = "Chuột có dây, không dây, siêu nhẹ và silent",
            Icon = "🖱️",
            SortOrder = 2,
            ParentCategory = ngoaiVi
        };
        var taiNghe = new Category
        {
            Name = "Tai nghe gaming",
            Slug = "tai-nghe-gaming",
            Description = "Tai nghe chụp tai, nhét tai và soundcard",
            Icon = "🎧",
            SortOrder = 3,
            ParentCategory = ngoaiVi
        };
        var lotChuot = new Category
        {
            Name = "Lót chuột",
            Slug = "lot-chuot",
            Description = "Lót chuột nhỏ, XXL Deskmat, lót cứng và LED RGB",
            Icon = "🟦",
            SortOrder = 4,
            ParentCategory = ngoaiVi
        };
        var tayCam = new Category
        {
            Name = "Tay cầm chơi game",
            Slug = "tay-cam-choi-game",
            Description = "Tay cầm PC/Xbox/PlayStation, vô lăng, cần lái",
            Icon = "🕹️",
            SortOrder = 5,
            ParentCategory = ngoaiVi
        };

        // ── Group 2: Linh kiện & Setup góc máy ──
        var linhKien = new Category
        {
            Name = "Linh kiện & Setup góc máy",
            Slug = "linh-kien-setup",
            Description = "Ghế, bàn và phụ kiện trang trí góc máy",
            Icon = "🖥️",
            SortOrder = 2
        };
        var gheGaming = new Category
        {
            Name = "Ghế gaming",
            Slug = "ghe-gaming",
            Description = "Ghế công thái học, ghế da PU/Mesh và ghế LED",
            Icon = "🪑",
            SortOrder = 1,
            ParentCategory = linhKien
        };
        var banGaming = new Category
        {
            Name = "Bàn gaming",
            Slug = "ban-gaming",
            Description = "Bàn chữ Z, chữ K và bàn nâng hạ thông minh",
            Icon = "🗄️",
            SortOrder = 2,
            ParentCategory = linhKien
        };
        var giadoArm = new Category
        {
            Name = "Giá đỡ & Arm",
            Slug = "gia-do-arm",
            Description = "Arm màn hình, giá đỡ tai nghe và điện thoại",
            Icon = "📺",
            SortOrder = 3,
            ParentCategory = linhKien
        };
        var anhSang = new Category
        {
            Name = "Ánh sáng & Trang trí",
            Slug = "anh-sang-trang-tri",
            Description = "Đèn LED dây RGB, Screenbar và đèn tam giác",
            Icon = "💡",
            SortOrder = 4,
            ParentCategory = linhKien
        };

        // ── Group 3: Stream & Audio ──
        var streamAudio = new Category
        {
            Name = "Phụ kiện Stream & Audio",
            Slug = "stream-audio",
            Description = "Micro, webcam và thiết bị capture stream",
            Icon = "🎧",
            SortOrder = 3
        };
        var microphone = new Category
        {
            Name = "Microphone",
            Slug = "microphone",
            Description = "Mic thu âm chuyên nghiệp, pop filter, arm treo mic",
            Icon = "🎙️",
            SortOrder = 1,
            ParentCategory = streamAudio
        };
        var webcam = new Category
        {
            Name = "Webcam & Đèn",
            Slug = "webcam-den",
            Description = "Webcam Full HD/4K và đèn LED ring light",
            Icon = "📷",
            SortOrder = 2,
            ParentCategory = streamAudio
        };
        var capture = new Category
        {
            Name = "Thiết bị Capture",
            Slug = "thiet-bi-capture",
            Description = "Capture card để stream từ console hoặc 2 PC",
            Icon = "📡",
            SortOrder = 3,
            ParentCategory = streamAudio
        };

        // ── Group 4: Vệ sinh & Bảo dưỡng ──
        var veSinh = new Category
        {
            Name = "Vệ sinh & Bảo dưỡng",
            Slug = "ve-sinh-bao-duong",
            Description = "Dụng cụ vệ sinh và bảo dưỡng thiết bị gaming",
            Icon = "🧼",
            SortOrder = 4
        };
        var dungCuVeSinh = new Category
        {
            Name = "Dụng cụ vệ sinh bàn phím",
            Slug = "dung-cu-ve-sinh",
            Description = "Keycap puller, cọ quét, bóng thổi bụi",
            Icon = "🧹",
            SortOrder = 1,
            ParentCategory = veSinh
        };
        var gelVeSinh = new Category
        {
            Name = "Gel vệ sinh & Dung dịch",
            Slug = "gel-ve-sinh",
            Description = "Gel vệ sinh bụi và dung dịch lau màn hình",
            Icon = "🧴",
            SortOrder = 2,
            ParentCategory = veSinh
        };
        var lubeLube = new Category
        {
            Name = "Bộ lube switch",
            Slug = "bo-lube-switch",
            Description = "Mỡ Krytox, cọ lube và switch opener",
            Icon = "🔧",
            SortOrder = 3,
            ParentCategory = veSinh
        };

        // ── Group 5: Theo Hệ máy ──
        var heMay = new Category
        {
            Name = "Theo Hệ máy",
            Slug = "theo-he-may",
            Description = "Tìm nhanh phụ kiện tương thích với thiết bị của bạn",
            Icon = "⚡",
            SortOrder = 5
        };
        var pcGaming = new Category
        {
            Name = "PC Gaming",
            Slug = "pc-gaming",
            Description = "Phụ kiện dành cho game thủ PC",
            Icon = "🖥️",
            SortOrder = 1,
            ParentCategory = heMay
        };
        var console = new Category
        {
            Name = "Console",
            Slug = "console",
            Description = "Phụ kiện PS5, Xbox Series X/S, Nintendo Switch",
            Icon = "🎮",
            SortOrder = 2,
            ParentCategory = heMay
        };
        var mobile = new Category
        {
            Name = "Mobile Gaming",
            Slug = "mobile-gaming",
            Description = "Nút chơi game, tản nhiệt và tay cầm điện thoại",
            Icon = "📱",
            SortOrder = 3,
            ParentCategory = heMay
        };

        // ── Group 6: Theo Hệ màu/Chủ đề ──
        var chuDe = new Category
        {
            Name = "Theo Hệ màu / Chủ đề",
            Slug = "theo-chu-de",
            Description = "Đồng bộ góc máy theo tone màu yêu thích",
            Icon = "🎨",
            SortOrder = 6
        };
        var pinkCyber = new Category
        {
            Name = "Pink Cyber",
            Slug = "pink-cyber",
            Description = "Góc máy màu hồng/vàng dễ thương",
            Icon = "🌸",
            SortOrder = 1,
            ParentCategory = chuDe
        };
        var totalBlack = new Category
        {
            Name = "Total Black",
            Slug = "total-black",
            Description = "Phong cách tối giản, đen huyền bí",
            Icon = "⬛",
            SortOrder = 2,
            ParentCategory = chuDe
        };
        var snowWhite = new Category
        {
            Name = "Snow White",
            Slug = "snow-white",
            Description = "Góc máy trắng tinh khôi",
            Icon = "⬜",
            SortOrder = 3,
            ParentCategory = chuDe
        };
        var rgbMini = new Category
        {
            Name = "RGB Minimalist",
            Slug = "rgb-minimalist",
            Description = "Đơn giản nhưng phải có đèn đổi màu",
            Icon = "🌈",
            SortOrder = 4,
            ParentCategory = chuDe
        };

        // ── Group 7: Danh mục đặc biệt ──
        var dacBiet = new Category
        {
            Name = "Danh mục đặc biệt",
            Slug = "danh-muc-dac-biet",
            Description = "Combo, sản phẩm mới và deal giá tốt",
            Icon = "🔥",
            SortOrder = 7
        };
        var buildSetup = new Category
        {
            Name = "Gợi ý góc máy",
            Slug = "build-your-setup",
            Description = "Combo Chuột + Phím + Tai nghe giá ưu đãi",
            Icon = "⚙️",
            SortOrder = 1,
            ParentCategory = dacBiet
        };
        var newArrivals = new Category
        {
            Name = "Hàng mới về",
            Slug = "new-arrivals",
            Description = "Sản phẩm công nghệ và trend mới nhất",
            Icon = "✨",
            SortOrder = 2,
            ParentCategory = dacBiet
        };
        var hotDeals = new Category
        {
            Name = "Săn Deal hot",
            Slug = "hot-deals",
            Description = "Hàng giảm giá, xả kho giá siêu tốt",
            Icon = "🏷️",
            SortOrder = 3,
            ParentCategory = dacBiet
        };

        context.Categories.AddRange(
            ngoaiVi, banPhim, chuot, taiNghe, lotChuot, tayCam,
            linhKien, gheGaming, banGaming, giadoArm, anhSang,
            streamAudio, microphone, webcam, capture,
            veSinh, dungCuVeSinh, gelVeSinh, lubeLube,
            heMay, pcGaming, console, mobile,
            chuDe, pinkCyber, totalBlack, snowWhite, rgbMini,
            dacBiet, buildSetup, newArrivals, hotDeals
        );
        await context.SaveChangesAsync();

        return (banPhim, chuot, taiNghe, gheGaming);
    }

    private static async Task ClearProductDataAsync(ApplicationDbContext context)
    {
        var cartItems = await context.CartItems.ToListAsync();
        context.CartItems.RemoveRange(cartItems);

        var orderItems = await context.OrderItems.ToListAsync();
        context.OrderItems.RemoveRange(orderItems);

        var orders = await context.Orders.ToListAsync();
        context.Orders.RemoveRange(orders);

        var specs = await context.ProductSpecifications.ToListAsync();
        context.ProductSpecifications.RemoveRange(specs);

        var products = await context.Products.ToListAsync();
        context.Products.RemoveRange(products);

        var categories = await context.Categories.ToListAsync();
        context.Categories.RemoveRange(categories);

        var brands = await context.Brands.ToListAsync();
        context.Brands.RemoveRange(brands);

        await context.SaveChangesAsync();
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        foreach (var role in new[] { "Admin", "Manager", "Staff", "Customer" })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    private static async Task SeedAdminAsync(UserManager<ApplicationUser> userManager)
    {
        await EnsureUser(userManager, "admin@nexusgear.local", "Store Administrator", "Admin@123", "Admin");
        await EnsureUser(userManager, "manager@nexusgear.local", "Nguyễn Quản Lý", "Manager@123", "Manager");
        await EnsureUser(userManager, "staff@nexusgear.local", "Trần Nhân Viên", "Staff@123", "Staff");
    }

    private static async Task EnsureUser(UserManager<ApplicationUser> userManager,
        string email, string fullName, string password, string role)
    {
        if (await userManager.FindByEmailAsync(email) != null) return;
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = fullName
        };
        await userManager.CreateAsync(user, password);
        await userManager.AddToRoleAsync(user, role);
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
