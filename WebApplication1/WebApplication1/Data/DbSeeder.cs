using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;
using WebApplication1.Services;

namespace WebApplication1.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        // Tự động sao chép placeholder.svg từ thư mục products sang thư mục uploads nếu chưa tồn tại
        var env = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();
        var uploadsPath = System.IO.Path.Combine(env.WebRootPath, "images", "uploads");
        System.IO.Directory.CreateDirectory(uploadsPath);
        var sourceFile = System.IO.Path.Combine(env.WebRootPath, "images", "products", "placeholder.svg");
        var destFile = System.IO.Path.Combine(uploadsPath, "placeholder.svg");
        if (System.IO.File.Exists(sourceFile) && !System.IO.File.Exists(destFile))
        {
            System.IO.File.Copy(sourceFile, destFile);
        }

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

        // Cập nhật hình ảnh thực cho các sản phẩm hiện có
        await UpdateProductImagesAsync(context);

        // Skip nếu danh mục đã được seed (có Icon)
        if (await context.Categories.AnyAsync(c => c.Icon != null))
        {
            // Thêm brands và sản phẩm mới nếu thiếu
            await SeedAdditionalDataAsync(context);
            return;
        }

        // Clear old data for clean re-seed
        await ClearProductDataAsync(context);

        // Seed category hierarchy
        var (banPhim, chuot, taiNghe, gheGaming) = await SeedCategoriesAsync(context);

        // Lấy tất cả category IDs theo slug
        var catMap = await context.Categories
            .Where(c => c.Slug != null)
            .ToDictionaryAsync(c => c.Slug!, c => c.Id);

        // Seed tất cả brands
        var brands = new[]
        {
            new Brand { Name = "Razer" },
            new Brand { Name = "Logitech" },
            new Brand { Name = "Corsair" },
            new Brand { Name = "Keychron" },
            new Brand { Name = "SteelSeries" },
            new Brand { Name = "HyperX" },
            new Brand { Name = "Sony" },
            new Brand { Name = "Secretlab" }
        };
        context.Brands.AddRange(brands);
        await context.SaveChangesAsync();

        // Pre-compute category IDs từ map
        var lotChuotId  = catMap.GetValueOrDefault("lot-chuot",        banPhim.Id);
        var micCatId    = catMap.GetValueOrDefault("microphone",        banPhim.Id);
        var webcamCatId = catMap.GetValueOrDefault("webcam-den",        banPhim.Id);
        var tayCamCatId = catMap.GetValueOrDefault("tay-cam-choi-game", banPhim.Id);

        var products = new List<Product>
        {
            // Chuột gaming
            CreateProduct("Razer DeathAdder V3 Pro", "Chuột gaming không dây với cảm biến Focus Pro 30K, thiết kế ergonomic cho tay phải, trọng lượng siêu nhẹ 63g.", 2890000, 25,
                chuot.Id, brands[0].Id, "/images/uploads/razer-deathadder-v3-pro.webp",
                "/images/uploads/razer-deathadder-v3-pro.webp",
                ("DPI", "30000"), ("Kết nối", "Wireless 2.4GHz"), ("Trọng lượng", "63g"), ("Pin", "90 giờ")),
            CreateProduct("Logitech G Pro X Superlight 2", "Chuột không dây siêu nhẹ 60g dành cho esport chuyên nghiệp. Cảm biến HERO 2 32K DPI, thiết kế đối xứng.", 3490000, 18,
                chuot.Id, brands[1].Id, "/images/uploads/logitech-gpx-superlight2.png",
                "/images/uploads/logitech-gpx-superlight2.png",
                ("DPI", "32000"), ("Kết nối", "LIGHTSPEED Wireless"), ("Trọng lượng", "60g"), ("Pin", "95 giờ")),

            // Bàn phím
            CreateProduct("Corsair K70 RGB PRO", "Bàn phím cơ gaming Full-size với switch Cherry MX Red, đèn LED RGB per-key, keycap PBT double-shot.", 4290000, 12,
                banPhim.Id, brands[2].Id, "/images/uploads/corsair-k70-rgb-pro.webp",
                "/images/uploads/corsair-k70-rgb-pro.webp",
                ("Switch", "Cherry MX Red"), ("Kết nối", "USB có dây"), ("Layout", "Full-size 104 phím"), ("Keycap", "PBT Double-Shot")),
            CreateProduct("Keychron Q1 Pro", "Bàn phím cơ không dây 75% cao cấp. Gasket mount giảm rung, hotswappable, tương thích QMK/VIA.", 3990000, 20,
                banPhim.Id, brands[3].Id, "/images/uploads/keychron-q1-pro.jpg",
                "/images/uploads/keychron-q1-pro.jpg",
                ("Switch", "Gateron G Pro Brown"), ("Kết nối", "Bluetooth 5.1 / USB-C"), ("Layout", "75% (84 phím)"), ("Hotswap", "Có")),
            CreateProduct("Razer Huntsman V3 Pro", "Bàn phím gaming optical analog cho phép điều chỉnh khoảng actuation 0.1–4.0mm theo từng game.", 5990000, 10,
                banPhim.Id, brands[0].Id, "/images/uploads/razer-huntsman-v3-pro.webp",
                "/images/uploads/razer-huntsman-v3-pro.webp",
                ("Switch", "Analog Optical"), ("Kết nối", "USB có dây"), ("Layout", "Full-size 104 phím"), ("Actuation", "0.1–4.0mm tùy chỉnh")),

            // Tai nghe gaming
            CreateProduct("Razer BlackShark V2 Pro", "Tai nghe gaming không dây chuẩn THX Spatial Audio 7.1 cho trải nghiệm âm thanh vòm thực tế.", 5490000, 15,
                taiNghe.Id, brands[0].Id, "/images/uploads/razer-blackshark-v2-pro.webp",
                "/images/uploads/razer-blackshark-v2-pro.webp",
                ("Kết nối", "Wireless 2.4GHz + Bluetooth"), ("Driver", "50mm Titanium"), ("Pin", "70 giờ"), ("Mic", "Siêu tim tháo rời")),
            CreateProduct("Logitech G733 Lightspeed", "Tai nghe không dây RGB siêu nhẹ 278g, kết nối LIGHTSPEED 2.4GHz, pin 29 giờ, màu sắc thời trang.", 2990000, 22,
                taiNghe.Id, brands[1].Id, "/images/uploads/logitech-g733.png",
                "/images/uploads/logitech-g733.png",
                ("Kết nối", "LIGHTSPEED 2.4GHz"), ("Driver", "40mm"), ("Pin", "29 giờ"), ("Trọng lượng", "278g")),
            CreateProduct("SteelSeries Arctis Nova Pro Wireless", "Tai nghe cao cấp dual wireless (2.4GHz + Bluetooth), 2 pin thay nổi tức thì, ANC chủ động, màn hình OLED điều khiển.", 8990000, 8,
                taiNghe.Id, brands[4].Id, "/images/uploads/placeholder.svg",
                "/images/uploads/placeholder.svg",
                ("Kết nối", "2.4GHz + Bluetooth 5.0"), ("Driver", "40mm Neodymium"), ("Pin", "Không giới hạn (2 pin thay)"), ("ANC", "Chủ động")),

            // Ghế gaming
            CreateProduct("Corsair T3 Rush", "Ghế gaming vải cao cấp, tay ghế 4D, bọc vải mềm thoáng khí. Tải trọng tối đa 120kg, ngả lưng 160°.", 8990000, 8,
                gheGaming.Id, brands[2].Id, "/images/uploads/corsair-t3-rush.webp",
                "/images/uploads/corsair-t3-rush.webp",
                ("Chất liệu", "Vải SoftWeave"), ("Tải trọng", "120kg"), ("Recline", "160°"), ("Armrest", "4D")),
            CreateProduct("Secretlab Titan Evo 2022", "Ghế gaming cao cấp với lumbar support 4 chiều tích hợp, gối đầu từ tính, da NEO Hybrid Leatherette siêu bền. Tải trọng 130kg.", 14990000, 5,
                gheGaming.Id, brands[7].Id, "/images/uploads/placeholder.svg",
                "/images/uploads/placeholder.svg",
                ("Chất liệu", "NEO Hybrid Leatherette"), ("Lumbar", "4 chiều tích hợp"), ("Tải trọng", "130kg"), ("Recline", "165°"), ("Armrest", "4D")),

            // Lót chuột
            CreateProduct("SteelSeries QcK Heavy XXL", "Lót chuột gaming XXL dày 6mm, bề mặt vải micro-woven tối ưu tốc độ, đế cao su chống trượt. Kích thước 900x300mm.", 890000, 40,
                lotChuotId, brands[4].Id,
                "/images/uploads/steelseries-qck-heavy-xxl.png", "/images/uploads/steelseries-qck-heavy-xxl.png",
                ("Kích thước", "900x300x6mm"), ("Chất liệu", "Micro-woven cloth"), ("Đế", "Cao su chống trượt")),
            CreateProduct("Corsair MM350 Champion Series XL", "Lót chuột XL với vải micro-weave cao cấp, tối ưu cho cả chuột quang và laser. Đế cao su 3mm. Kích thước 450x400mm.", 990000, 25,
                lotChuotId, brands[2].Id,
                "/images/uploads/placeholder.svg", "/images/uploads/placeholder.svg",
                ("Kích thước", "450x400x3mm"), ("Chất liệu", "Micro-weave cloth"), ("Đế", "Cao su 3mm"), ("Tối ưu cho", "Quang + Laser")),
            CreateProduct("Razer Gigantus V2 XXL", "Lót chuột XXL vi sợi mịn tối ưu tốc độ và kiểm soát. Đế cao su dày 4mm. Kích thước 940x410mm bao phủ toàn bộ setup.", 790000, 35,
                lotChuotId, brands[0].Id,
                "/images/uploads/placeholder.svg", "/images/uploads/placeholder.svg",
                ("Kích thước", "940x410x4mm"), ("Chất liệu", "Vi sợi mịn"), ("Đế", "Cao su dày anti-slip")),

            // Microphone
            CreateProduct("HyperX QuadCast S", "Micro USB condenser RGB với 4 chế độ pickup pattern, chống rung tích hợp, nút tắt tiếng nhạy cảm. Lý tưởng cho streaming.", 3290000, 15,
                micCatId, brands[5].Id,
                "/images/uploads/hyperx-quadcast-s.jpg", "/images/uploads/hyperx-quadcast-s.jpg",
                ("Loại", "USB Condenser"), ("Polar Pattern", "4 chế độ"), ("Tần số đáp", "20Hz–20kHz"), ("RGB", "Có")),
            CreateProduct("Razer Seiren V3 Chroma", "Micro gaming USB với đèn Chroma RGB phản ứng theo âm thanh (Stream Reactive). Capsule supercardioid 25mm khử noise tốt.", 3490000, 12,
                micCatId, brands[0].Id,
                "/images/uploads/razer-seiren-v3-chroma.webp", "/images/uploads/razer-seiren-v3-chroma.webp",
                ("Loại", "USB Condenser"), ("Polar Pattern", "Supercardioid"), ("Chroma RGB", "Stream Reactive"), ("Kết nối", "USB-C")),

            // Webcam
            CreateProduct("Logitech C922 Pro Stream", "Webcam stream Full HD 1080p 30fps / 720p 60fps, 2 micro stereo, tự động chỉnh sáng RightLight 2, hỗ trợ thay nền ảo.", 2290000, 20,
                webcamCatId, brands[1].Id,
                "/images/uploads/logitech-c922.png", "/images/uploads/logitech-c922.png",
                ("Độ phân giải", "1080p/30fps hoặc 720p/60fps"), ("Micro", "2 stereo tích hợp"), ("Góc nhìn", "78°"), ("Kết nối", "USB-A")),

            // Tay cầm
            CreateProduct("Sony DualSense Wireless Controller", "Tay cầm PS5 không dây với Adaptive Triggers và Haptic Feedback thế hệ mới. Pin 12h, sạc USB-C. Tương thích PC và PS5.", 1890000, 30,
                tayCamCatId, brands[6].Id,
                "/images/uploads/placeholder.svg", "/images/uploads/placeholder.svg",
                ("Kết nối", "Wireless Bluetooth / USB-C"), ("Adaptive Triggers", "Có"), ("Haptic Feedback", "Có"), ("Pin", "Khoảng 12 giờ")),
            CreateProduct("Logitech G923 TRUEFORCE Racing Wheel", "Vô lăng đua xe với TRUEFORCE phản hồi lực 1000Hz. Hỗ trợ PC và PS4/PS5. Góc quay 900°, pedal 2 bàn đạp.", 9990000, 5,
                tayCamCatId, brands[1].Id,
                "/images/uploads/placeholder.svg", "/images/uploads/placeholder.svg",
                ("Góc quay", "900°"), ("Kết nối", "USB"), ("Tương thích", "PC / PS4 / PS5"), ("Pedal", "2 bàn đạp")),
        };

        // Cập nhật ShippingClass cho hàng cồng kềnh
        foreach (var p in products)
        {
            if (p.Name.Contains("Ghế") || p.Name == "Corsair T3 Rush" || p.Name == "Secretlab Titan Evo 2022")
                p.ShippingClass = ShippingClass.CongKenh;
            else if (p.Name.Contains("XXL") || p.Name.Contains("Racing Wheel") || p.Name == "Logitech G923 TRUEFORCE Racing Wheel")
                p.ShippingClass = ShippingClass.Vua;
        }

        context.Products.AddRange(products);
        await context.SaveChangesAsync();
    }

    private static async Task UpdateProductImagesAsync(ApplicationDbContext context)
    {
        var imageMap = new Dictionary<string, string>
        {
            ["Razer DeathAdder V3 Pro"]      = "/images/uploads/razer-deathadder-v3-pro.webp",
            ["Logitech G Pro X Superlight 2"]= "/images/uploads/logitech-gpx-superlight2.png",
            ["Corsair K70 RGB PRO"]           = "/images/uploads/corsair-k70-rgb-pro.webp",
            ["Keychron Q1 Pro"]               = "/images/uploads/keychron-q1-pro.jpg",
            ["Razer BlackShark V2 Pro"]       = "/images/uploads/razer-blackshark-v2-pro.webp",
            ["Logitech G733 Lightspeed"]      = "/images/uploads/logitech-g733.png",
            ["Corsair T3 Rush"]               = "/images/uploads/corsair-t3-rush.webp",
            ["Razer Huntsman V3 Pro"]         = "/images/uploads/razer-huntsman-v3-pro.webp",
            ["SteelSeries QcK Heavy XXL"]     = "/images/uploads/steelseries-qck-heavy-xxl.png",
            ["HyperX QuadCast S"]             = "/images/uploads/hyperx-quadcast-s.jpg",
            ["Razer Seiren V3 Chroma"]        = "/images/uploads/razer-seiren-v3-chroma.webp",
            ["Logitech C922 Pro Stream"]      = "/images/uploads/logitech-c922.png",
            ["Corsair MM350 Champion Series XL"]      = "/images/uploads/corsair-mm350-xl.webp",
            ["Razer Gigantus V2 XXL"]                 = "/images/uploads/razer-gigantus-v2-xxl.webp",
            ["Sony DualSense Wireless Controller"]    = "/images/uploads/sony-dualsense.png",
            ["Logitech G923 TRUEFORCE Racing Wheel"]  = "/images/uploads/logitech-g923.png",
            ["Secretlab Titan Evo 2022"]              = "/images/uploads/secretlab-titan-evo-2022.jpg",
            ["ASUS ROG Gladius III Wireless"]         = "/images/uploads/asus-rog-gladius3-wireless.webp",
            ["Xbox Wireless Controller"]              = "/images/uploads/xbox-wireless-controller.jpg",
            ["Corsair Virtuoso RGB Wireless XT"]      = "/images/uploads/corsair-virtuoso-rgb-wireless.webp",
            ["Blue Yeti X USB Microphone"]            = "/images/uploads/blue-yeti-x.png",
            ["Razer Kiyo Pro Streaming Webcam"]       = "/images/uploads/razer-kiyo-pro.webp",
            ["DXRacer Formula Series F11"]            = "/images/uploads/dxracer-formula-gaming-chair.png",
            ["Ducky One 3 Mini"]                      = "/images/uploads/ducky-one3-mini.jpg",
        };

        var products = await context.Products.Where(p => imageMap.Keys.Contains(p.Name)).ToListAsync();
        foreach (var p in products)
        {
            if (imageMap.TryGetValue(p.Name, out var img) && (p.ImageUrl ?? "").Contains("placeholder"))
            {
                p.ImageUrl = img;
                p.SecondaryImageUrls = img;
            }
        }
        if (products.Any(p => context.Entry(p).State == Microsoft.EntityFrameworkCore.EntityState.Modified))
            await context.SaveChangesAsync();
    }

    private static async Task SeedAdditionalDataAsync(ApplicationDbContext context)
    {
        // Thêm brands mới nếu chưa có
        var brandNames = new[] { "SteelSeries", "HyperX", "Sony", "Secretlab", "ASUS ROG", "Microsoft", "Blue", "DXRacer", "Ducky", "Eureka" };
        foreach (var name in brandNames)
        {
            if (!await context.Brands.AnyAsync(b => b.Name == name))
                context.Brands.Add(new Brand { Name = name });
        }
        await context.SaveChangesAsync();

        var brandMap = await context.Brands.ToDictionaryAsync(b => b.Name, b => b.Id);
        var catMap   = await context.Categories
            .Where(c => c.Slug != null)
            .ToDictionaryAsync(c => c.Slug!, c => c.Id);

        var newProducts = new (string Name, string Desc, decimal Price, int Stock, string CatSlug, string BrandName, string Img, ShippingClass Ship, (string, string)[] Specs)[]
        {
            // ── Sản phẩm cũ (giữ lại để không bị mất nếu DB chưa có) ──
            ("SteelSeries QcK Heavy XXL", "Lót chuột gaming XXL dày 6mm, vải micro-woven, đế cao su chống trượt. Kích thước 900x300mm.", 890000, 40, "lot-chuot", "SteelSeries", "/images/uploads/steelseries-qck-heavy-xxl.png", ShippingClass.Vua,
             [("Kích thước","900x300x6mm"),("Chất liệu","Micro-woven cloth"),("Đế","Cao su chống trượt")]),
            ("Corsair MM350 Champion Series XL", "Lót chuột XL vải micro-weave chống đổ nước, tối ưu cho chuột quang + laser. Kích thước 450x400mm.", 990000, 25, "lot-chuot", "Corsair", "/images/uploads/corsair-mm350-xl.webp", ShippingClass.Vua,
             [("Kích thước","450x400x3mm"),("Chất liệu","Micro-weave cloth"),("Đặc tính","Chống đổ nước")]),
            ("Razer Gigantus V2 XXL", "Lót chuột XXL vi sợi mịn, đế cao su dày 4mm. Kích thước 940x410mm bao phủ toàn bộ setup.", 790000, 35, "lot-chuot", "Razer", "/images/uploads/razer-gigantus-v2-xxl.webp", ShippingClass.Vua,
             [("Kích thước","940x410x4mm"),("Chất liệu","Vi sợi mịn"),("Đế","Cao su dày anti-slip")]),
            ("HyperX QuadCast S", "Micro USB condenser RGB, 4 chế độ pickup, chống rung tích hợp. Lý tưởng cho streaming và podcast.", 3290000, 15, "microphone", "HyperX", "/images/uploads/hyperx-quadcast-s.jpg", ShippingClass.Nho,
             [("Loại","USB Condenser"),("Polar Pattern","4 chế độ"),("Tần số đáp","20Hz–20kHz"),("RGB","Có")]),
            ("Razer Seiren V3 Chroma", "Micro gaming USB, đèn Chroma RGB Stream Reactive, capsule supercardioid 25mm khử noise tốt.", 3490000, 12, "microphone", "Razer", "/images/uploads/razer-seiren-v3-chroma.webp", ShippingClass.Nho,
             [("Loại","USB Condenser"),("Polar Pattern","Supercardioid"),("Chroma RGB","Stream Reactive"),("Kết nối","USB-C")]),
            ("Logitech C922 Pro Stream", "Webcam stream Full HD 1080p/30fps hay 720p/60fps, 2 micro stereo, tự động chỉnh sáng RightLight 2.", 2290000, 20, "webcam-den", "Logitech", "/images/uploads/logitech-c922.png", ShippingClass.Nho,
             [("Độ phân giải","1080p/30fps hoặc 720p/60fps"),("Micro","2 stereo tích hợp"),("Góc nhìn","78°"),("Kết nối","USB-A")]),
            ("Sony DualSense Wireless Controller", "Tay cầm PS5 không dây, Adaptive Triggers + Haptic Feedback thế hệ mới. Pin 12h, sạc USB-C. Dùng cho PC và PS5.", 1890000, 30, "tay-cam-choi-game", "Sony", "/images/uploads/sony-dualsense.png", ShippingClass.Nho,
             [("Kết nối","Wireless Bluetooth / USB-C"),("Adaptive Triggers","Có"),("Haptic Feedback","Có"),("Pin","Khoảng 12 giờ")]),
            ("Logitech G923 TRUEFORCE Racing Wheel", "Vô lăng đua xe TRUEFORCE phản hồi lực 1000Hz. Hỗ trợ PC/PS4/PS5. Góc quay 900°, 2 bàn đạp.", 9990000, 5, "tay-cam-choi-game", "Logitech", "/images/uploads/logitech-g923.png", ShippingClass.Vua,
             [("Góc quay","900°"),("Kết nối","USB"),("Tương thích","PC / PS4 / PS5"),("Pedal","2 bàn đạp")]),
            ("SteelSeries Arctis Nova Pro Wireless", "Tai nghe cao cấp dual wireless (2.4GHz + Bluetooth), 2 pin thay nổi, ANC chủ động, màn OLED điều khiển.", 8990000, 8, "tai-nghe-gaming", "SteelSeries", "/images/uploads/placeholder.svg", ShippingClass.Nho,
             [("Kết nối","2.4GHz + Bluetooth 5.0"),("Driver","40mm Neodymium"),("Pin","Không giới hạn (2 pin thay)"),("ANC","Chủ động")]),
            ("Secretlab Titan Evo 2022", "Ghế gaming cao cấp, lumbar support 4 chiều tích hợp, gối đầu từ tính, da NEO Hybrid Leatherette. Tải trọng 130kg.", 14990000, 5, "ghe-gaming", "Secretlab", "/images/uploads/secretlab-titan-evo-2022.jpg", ShippingClass.CongKenh,
             [("Chất liệu","NEO Hybrid Leatherette"),("Lumbar","4 chiều tích hợp"),("Tải trọng","130kg"),("Recline","165°"),("Armrest","4D")]),

            // ── Sản phẩm mới thêm vào ──

            // Chuột gaming mới
            ("Logitech G502 X Plus Wireless", "Chuột gaming không dây cao cấp 89g, HERO 25K sensor, 13 nút lập trình, sạc POWERPLAY không dây khi chơi, đèn RGB.", 3990000, 20, "chuot-gaming", "Logitech", "/images/uploads/logitech-g502x-plus.png", ShippingClass.Nho,
             [("DPI","100–25600"),("Kết nối","LIGHTSPEED Wireless"),("Trọng lượng","89g"),("Pin","130 giờ")]),
            ("Razer Viper V3 HyperSpeed", "Chuột gaming không dây siêu nhẹ 82g chuẩn esport. Cảm biến Focus X 30K, kết nối HyperSpeed 2.4GHz, pin 280 giờ.", 1890000, 35, "chuot-gaming", "Razer", "/images/uploads/razer-viper-v3-hyperspeed.webp", ShippingClass.Nho,
             [("DPI","30000"),("Kết nối","HyperSpeed Wireless 2.4GHz"),("Trọng lượng","82g"),("Pin","280 giờ")]),
            ("ASUS ROG Gladius III Wireless", "Chuột gaming không dây 3-in-1 (2.4GHz + Bluetooth + có dây), cảm biến ROG AimPoint 36K, tay phải ergonomic. Socket switch dễ thay.", 2490000, 18, "chuot-gaming", "ASUS ROG", "/images/uploads/asus-rog-gladius3-wireless.webp", ShippingClass.Nho,
             [("DPI","36000"),("Kết nối","2.4GHz / Bluetooth / USB"),("Switch","Thay được không hàn"),("Pin","100 giờ")]),

            // Bàn phím gaming mới
            ("Ducky One 3 Mini", "Bàn phím cơ 60% cao cấp, hotswappable PCB, RGB per-key, chất liệu PBT double-shot. Layout compact lý tưởng cho setup gọn.", 2690000, 25, "ban-phim-gaming", "Ducky", "/images/uploads/ducky-one3-mini.jpg", ShippingClass.Nho,
             [("Layout","60% (61 phím)"),("Switch","Cherry MX Red (hotswap)"),("Keycap","PBT Double-Shot"),("RGB","Per-key")]),
            ("SteelSeries Apex Pro TKL Wireless", "Bàn phím TKL không dây với OmniPoint 2.0 switch điều chỉnh actuation 0.2–3.8mm theo từng phím. Màn OLED thông minh.", 4990000, 10, "ban-phim-gaming", "SteelSeries", "/images/uploads/placeholder.svg", ShippingClass.Nho,
             [("Switch","OmniPoint 2.0 Adjustable"),("Actuation","0.2–3.8mm"),("Kết nối","2.4GHz / USB-C"),("Layout","TKL (87 phím)")]),

            // Tai nghe gaming mới
            ("HyperX Cloud II Wireless", "Tai nghe gaming không dây 7.1 virtual surround, driver 53mm, pin 30 giờ, micro detachable. Kết nối 2.4GHz USB.", 3290000, 20, "tai-nghe-gaming", "HyperX", "/images/uploads/hyperx-cloud2-wireless.jpg", ShippingClass.Nho,
             [("Kết nối","Wireless 2.4GHz"),("Driver","53mm"),("Âm thanh","7.1 Virtual Surround"),("Pin","30 giờ")]),
            ("Corsair Virtuoso RGB Wireless XT", "Tai nghe gaming flagship không dây dual mode (2.4GHz + Bluetooth), driver 50mm neodymium, chứng nhận Dolby Atmos & THX Spatial.", 5490000, 12, "tai-nghe-gaming", "Corsair", "/images/uploads/corsair-virtuoso-rgb-wireless.webp", ShippingClass.Nho,
             [("Kết nối","2.4GHz + Bluetooth"),("Driver","50mm Neodymium"),("Chuẩn âm","Dolby Atmos + THX Spatial"),("Pin","20 giờ")]),

            // Ghế gaming mới
            ("DXRacer Formula Series F11", "Ghế gaming da PU cổ điển của DXRacer, khung thép vững chắc. Hỗ trợ chiều cao 155–185cm, tải trọng 100kg, ngả lưng 135°.", 6990000, 10, "ghe-gaming", "DXRacer", "/images/uploads/dxracer-formula-gaming-chair.png", ShippingClass.CongKenh,
             [("Chất liệu","Da PU"),("Tải trọng","100kg"),("Recline","135°"),("Chiều cao hỗ trợ","155–185cm")]),

            // Bàn gaming (thể loại mới có sản phẩm)
            ("Eureka Ergonomic Z1-S Gaming Desk", "Bàn gaming hình chữ Z, mặt bàn carbon fiber 140x60cm, móc tai nghe + USB hub tích hợp, chân thép sơn tĩnh điện, tải 150kg.", 4990000, 8, "ban-gaming", "Eureka", "/images/uploads/placeholder.svg", ShippingClass.CongKenh,
             [("Kích thước mặt bàn","140x60cm"),("Chất liệu","Carbon Fiber"),("Tính năng","USB hub + móc tai nghe"),("Tải trọng","150kg")]),

            // Lót chuột mới (RGB)
            ("HyperX Fury Ultra XL RGB", "Lót chuột XL 900x300mm có đèn LED RGB viền, bề mặt vải tốc độ cao, đế cao su 3mm chống trượt, sạc USB.", 1190000, 28, "lot-chuot", "HyperX", "/images/uploads/placeholder.svg", ShippingClass.Vua,
             [("Kích thước","900x300x3mm"),("RGB","Viền LED RGB"),("Bề mặt","Vải tốc độ cao"),("Kết nối","USB sạc")]),

            // Tay cầm mới
            ("Xbox Wireless Controller", "Tay cầm Xbox chính hãng Microsoft cho PC/Xbox Series X|S/Xbox One. Kết nối không dây Xbox hoặc Bluetooth, pin 40 giờ.", 1690000, 45, "tay-cam-choi-game", "Microsoft", "/images/uploads/xbox-wireless-controller.jpg", ShippingClass.Nho,
             [("Kết nối","Xbox Wireless / Bluetooth"),("Tương thích","Xbox Series X|S / Xbox One / PC"),("Pin","40 giờ (AA)"),("Màu","Carbon Black")]),

            // Microphone mới
            ("Blue Yeti X USB Microphone", "Micro thu âm USB professional với 4 polar pattern, Led VU meter real-time, tích hợp phần mềm Blue Sherpa. Lý tưởng cho podcast, streaming.", 3990000, 12, "microphone", "Blue", "/images/uploads/blue-yeti-x.png", ShippingClass.Nho,
             [("Loại","USB Condenser"),("Polar Pattern","Cardioid, Bidirectional, Omnidirectional, Stereo"),("VU Meter","LED real-time"),("Kết nối","USB")]),

            // Webcam mới
            ("Razer Kiyo Pro Streaming Webcam", "Webcam stream 1080p 60fps với cảm biến CMOS lớn 1/2.8\", góc nhìn rộng 103° điều chỉnh, tương thích OBS/XSplit.", 3190000, 15, "webcam-den", "Razer", "/images/uploads/razer-kiyo-pro.webp", ShippingClass.Nho,
             [("Độ phân giải","1080p/60fps"),("Cảm biến","SONY IMX415 1/2.8\""),("Góc nhìn","103° điều chỉnh"),("Kết nối","USB-C")]),
        };

        foreach (var (name, desc, price, stock, catSlug, brandName, img, ship, specs) in newProducts)
        {
            if (await context.Products.AnyAsync(p => p.Name == name)) continue;
            if (!catMap.TryGetValue(catSlug, out var catId)) continue;
            if (!brandMap.TryGetValue(brandName, out var brandId)) continue;

            var p = CreateProduct(name, desc, price, stock, catId, brandId, img, img,
                specs.Select(s => (s.Item1, s.Item2)).ToArray());
            p.ShippingClass = ship;
            context.Products.Add(p);
        }
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
        int categoryId, int brandId, string imageUrl, string? secondaryImageUrls, params ValueTuple<string, string>[] specs)
    {
        var product = new Product
        {
            Name = name,
            Description = description,
            Price = price,
            Stock = stock,
            CategoryId = categoryId,
            BrandId = brandId,
            ImageUrl = imageUrl,
            SecondaryImageUrls = secondaryImageUrls
        };
        foreach (var (key, value) in specs)
        {
            product.Specifications.Add(new ProductSpecification { Key = key, Value = value });
        }
        return product;
    }
}
