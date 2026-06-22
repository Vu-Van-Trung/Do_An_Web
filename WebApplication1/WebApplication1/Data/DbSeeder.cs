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

        // Skip nếu danh mục đã được seed (có Icon)
        if (await context.Categories.AnyAsync(c => c.Icon != null))
        {
            await SeedAdditionalDataAsync(context);
        }
        else
        {

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

        await EnsureProductsPerCategoryAsync(context);
        await EnrichAllProductImagesAsync(context);
    }

    private static async Task EnsureProductsPerCategoryAsync(ApplicationDbContext context)
    {
        var brandNames = new[]
        {
            "BenQ", "Govee", "Elgato", "AVerMedia", "Glorious", "ColorCoral", "Whoosh", "Krytox",
            "Nintendo", "8BitDo", "GameSir", "NZXT", "Cooler Master", "Arozzi", "Thermaltake",
            "Redragon", "Creative", "HAGiBiS", "Ergotron", "Nanoleaf"
        };
        foreach (var name in brandNames)
        {
            if (!await context.Brands.AnyAsync(b => b.Name == name))
                context.Brands.Add(new Brand { Name = name });
        }
        await context.SaveChangesAsync();

        var brandMap = await context.Brands.ToDictionaryAsync(b => b.Name, b => b.Id);
        var catMap = await context.Categories
            .Where(c => c.Slug != null && c.ParentCategoryId != null)
            .ToDictionaryAsync(c => c.Slug!, c => c.Id);

        var imgA = "/images/uploads/43456556-fc6e-49e4-9372-6648bf58aa6c.jpg";
        var imgB = "/images/uploads/de1ae0d0-b711-45c8-b799-1a5ef3c462cb.jpg";
        var imgC = "/images/uploads/1f29ebc4-518e-452e-be74-5b7e71806138.jpg";
        var imgD = "/images/uploads/2185541f-2e12-4707-920b-07116fae04f4.jpg";

        var categoryProducts = new Dictionary<string, (string Name, string Desc, decimal Price, int Stock, string Brand, string Primary, string[] Secondary, ShippingClass Ship, (string, string)[] Specs)[]>
        {
            ["anh-sang-trang-tri"] =
            [
                ("BenQ ScreenBar Halo", "Đèn treo màn hình cao cấp với đèn nền RGB halo, cảm biến ánh sáng tự động, không chiếu lóa màn hình. Lý tưởng cho làm việc và chơi game ban đêm.", 4290000, 14, "BenQ", "/images/uploads/de1ae0d0-b711-45c8-b799-1a5ef3c462cb.jpg", [imgA, imgC], ShippingClass.Nho,
                    [("Công suất", "5W LED"), ("Điều khiển", "Cảm biến ánh sáng + touch"), ("Kết nối", "USB-C"), ("Tương thích", "Màn hình 0.7–2.5cm")]),
                ("Govee Glide Hexa RGBIC", "Đèn LED dán tường 10 miếng hexagon, đồng bộ nhạc và game qua app Govee Home. Hiệu ứng RGBIC chuyển màu mượt mà.", 2490000, 20, "Govee", imgA, [imgB, imgD], ShippingClass.Nho,
                    [("Số miếng", "10 hexagon"), ("Điều khiển", "App + Alexa/Google"), ("Công suất", "24W"), ("Kết nối", "Wi-Fi 2.4GHz")]),
                ("Razer Chroma Light Strip 2m", "Dải đèn LED RGB Chroma 2m gắn sau bàn hoặc màn hình, đồng bộ Razer Synapse với thiết bị Razer khác.", 1890000, 25, "Razer", "/images/uploads/razer-seiren-v3-chroma.webp", [imgA, imgB], ShippingClass.Nho,
                    [("Chiều dài", "2 mét"), ("LED", "RGB Chroma"), ("Điều khiển", "Razer Synapse"), ("Kết nối", "USB")]),
            ],
            ["ban-gaming"] =
            [
                ("Arozzi Arena Gaming Desk", "Bàn gaming chuyên dụng 160x80cm với lót vải full desk, khe quản lý dây, thiết kế cong ergonomic. Khung thép sơn tĩnh điện chịu lực 150kg.", 5990000, 6, "Arozzi", "/images/uploads/dxracer-formula-gaming-chair.png", [imgA, imgC], ShippingClass.CongKenh,
                    [("Kích thước", "160x80cm"), ("Chất liệu mặt", "Vải gaming"), ("Tải trọng", "150kg"), ("Màu", "Đen")]),
                ("Thermaltake Level 20 GT Battlestation", "Bàn gaming chữ K cao cấp với mặt bàn kính cường lực, LED RGB viền, giá đỡ tai nghe và cốc tích hợp. Kích thước 140x70cm.", 7490000, 5, "Thermaltake", "/images/uploads/corsair-t3-rush.webp", [imgB, imgD], ShippingClass.CongKenh,
                    [("Kích thước", "140x70cm"), ("Mặt bàn", "Kính cường lực"), ("RGB", "Viền LED"), ("Tải trọng", "120kg")]),
            ],
            ["gia-do-arm"] =
            [
                ("Ergotron LX Desk Monitor Arm", "Arm màn hình đơn hỗ trợ 17–32 inch, nâng hạ 33cm, xoay 360°, quản lý cáp tích hợp. Giải phóng không gian bàn làm việc.", 3290000, 18, "Ergotron", imgC, [imgA, imgB], ShippingClass.Vua,
                    [("Hỗ trợ màn hình", "17–32 inch"), ("Tải trọng", "3.2–11.3kg"), ("Nâng hạ", "33cm"), ("Bảo hành", "10 năm")]),
                ("Razer Base Station V2 Chroma", "Giá treo tai nghe RGB Chroma với 3 cổng USB 3.0 hub tích hợp. Chống trượt cao su, thiết kế gọn cho góc máy.", 1490000, 30, "Razer", "/images/uploads/razer-blackshark-v2-pro.webp", [imgA, imgD], ShippingClass.Nho,
                    [("Hub USB", "3 cổng USB 3.0"), ("RGB", "Chroma"), ("Chất liệu", "Nhựa + cao su"), ("Kích thước", "Compact")]),
                ("Elgato Wave Mic Arm LP", "Cánh tay treo micro low-profile với tầm với 70cm, xoay linh hoạt, quản lý cáp ẩn. Tương thích hầu hết micro có ren 3/8\" và 5/8\".", 2190000, 15, "Elgato", "/images/uploads/hyperx-quadcast-s.jpg", [imgB, imgC], ShippingClass.Nho,
                    [("Tầm với", "70cm"), ("Ren", "3/8\" / 5/8\" adapter"), ("Chất liệu", "Kim loại"), ("Màu", "Đen matte")]),
            ],
            ["thiet-bi-capture"] =
            [
                ("Elgato Game Capture HD60 X", "Capture card USB 3.0 ghi hình 4K60 HDR10 passthrough, stream 1080p60. Tương thích OBS, Streamlabs và console PS5/Xbox/Switch.", 4490000, 12, "Elgato", imgD, [imgA, imgB], ShippingClass.Nho,
                    [("Passthrough", "4K60 HDR10"), ("Ghi hình", "1080p60"), ("Kết nối", "USB 3.0 / HDMI"), ("Độ trễ", "Ultra low latency")]),
                ("AVerMedia Live Gamer ULTRA GC553", "Capture card 4K30 ghi hình, 4K60 passthrough HDR, hỗ trợ party chat mix. Plug-and-play cho streamer console và PC.", 3990000, 10, "AVerMedia", imgA, [imgC, imgD], ShippingClass.Nho,
                    [("Passthrough", "4K60 HDR"), ("Ghi hình", "4K30 / 1080p60"), ("Kết nối", "USB 3.1 Gen1"), ("Phần mềm", "RECentral 4")]),
                ("Elgato Cam Link 4K", "Thiết bị capture biến máy ảnh DSLR/mirrorless thành webcam 4K. Kích thước nhỏ gọn, cắm USB 3.0 là dùng ngay.", 2790000, 14, "Elgato", "/images/uploads/razer-kiyo-pro.webp", [imgB, imgD], ShippingClass.Nho,
                    [("Độ phân giải", "4K30 / 1080p60"), ("Kết nối", "USB 3.0 / HDMI in"), ("Tương thích", "DSLR / Mirrorless"), ("Kích thước", "USB dongle")]),
            ],
            ["dung-cu-ve-sinh"] =
            [
                ("Glorious GMMK Keycap Puller Set", "Bộ dụng cụ tháo keycap và switch gồm puller thép, switch puller 2 đầu và hộp đựng keycap. Dành cho bàn phím cơ hotswap.", 290000, 50, "Glorious", imgB, [imgA, imgC], ShippingClass.Nho,
                    [("Bao gồm", "Keycap puller + switch puller"), ("Chất liệu", "Thép không gỉ"), ("Tương thích", "Cherry MX style"), ("Màu", "Đen")]),
                ("HAGiBiS Keyboard Cleaning Kit", "Bộ vệ sinh bàn phím gồm cọ lông mềm, bóng thổi khí, tăm bông và túi đựng. An toàn cho keycap PBT và switch.", 190000, 60, "HAGiBiS", imgC, [imgA, imgD], ShippingClass.Nho,
                    [("Bao gồm", "Cọ + bóng thổi + tăm bông"), ("An toàn", "Không xước keycap"), ("Kích thước", "Nhỏ gọn"), ("Màu", "Xám")]),
                ("OXO Good Grips Electronics Brush", "Cọ quét bụi điện tử đầu siêu mềm, cán ergonomic chống trượt. Làm sạch khe bàn phím và cổng USB an toàn.", 250000, 45, "HAGiBiS", imgA, [imgB, imgC], ShippingClass.Nho,
                    [("Đầu cọ", "Lông siêu mềm"), ("Cán", "Ergonomic anti-slip"), ("Dùng cho", "Bàn phím, laptop, màn hình"), ("Xuất xứ", "Thiết kế Mỹ")]),
            ],
            ["gel-ve-sinh"] =
            [
                ("ColorCoral Cleaning Gel 160g", "Gel vệ sinh bụi bàn phím, khe phím và ốp điện thoại. Dẻo dai, không để lại vết ướt, tái sử dụng nhiều lần.", 89000, 80, "ColorCoral", imgD, [imgA, imgB], ShippingClass.Nho,
                    [("Dung tích", "160g"), ("Công dụng", "Hút bụi khe phím"), ("Tái sử dụng", "Có"), ("Mùi", "Hương chanh nhẹ")]),
                ("Whoosh Screen Cleaner Kit", "Bộ lau màn hình chuyên dụng 500ml + khăn microfiber. Không cồn, an toàn cho màn hình gaming OLED và LCD.", 350000, 40, "Whoosh", imgA, [imgC, imgD], ShippingClass.Nho,
                    [("Dung tích", "500ml"), ("Khăn", "Microfiber 30x30cm"), ("Thành phần", "Không cồn, không ammonia"), ("Dùng cho", "Màn hình, kính, laptop")]),
                ("Gel vệ sinh bàn phím NexusGear 100g", "Gel làm sạch bụi bẩn khe phím, cổng sạc và tai nghe. Công thức không độc hại, an toàn cho thiết bị điện tử.", 69000, 100, "Glorious", imgB, [imgA, imgD], ShippingClass.Nho,
                    [("Dung tích", "100g"), ("Màu", "Vàng chanh"), ("Tái sử dụng", "3–5 lần"), ("Bảo quản", "Nơi khô ráo")]),
            ],
            ["bo-lube-switch"] =
            [
                ("Krytox 205g0 Lube Kit 5ml", "Bộ mỡ bôi trơn switch cao cấp Krytox 205 Grade 0 kèm cọ lube và switch opener. Giảm tiếng ồn, cải thiện cảm giác gõ cho switch linear.", 450000, 35, "Krytox", imgC, [imgA, imgB], ShippingClass.Nho,
                    [("Dung tích", "5ml"), ("Loại mỡ", "Krytox 205g0"), ("Bao gồm", "Cọ + opener"), ("Phù hợp", "Linear / Tactile switch")]),
                ("Glorious Switch Opener + Lube Station", "Bộ mở switch và giá đỡ lube 2-in-1 cho Cherry MX style. Giữ switch cố định khi bôi mỡ, dễ thao tác cho người mới.", 590000, 28, "Glorious", imgA, [imgB, imgD], ShippingClass.Nho,
                    [("Tương thích", "Cherry MX / Gateron / Kailh"), ("Chất liệu", "Nhựa ABS"), ("Bao gồm", "Opener + station"), ("Màu", "Trắng")]),
                ("Krytox GPL 105 Oil 2ml", "Dầu bôi trơn spring switch Krytox GPL 105, giảm ping và cải thiện độ mượt. Dùng kết hợp với 205g0 cho hiệu quả tối ưu.", 320000, 40, "Krytox", imgB, [imgC, imgD], ShippingClass.Nho,
                    [("Dung tích", "2ml"), ("Loại", "GPL 105 oil"), ("Dùng cho", "Spring switch"), ("Kết hợp", "Krytox 205g0")]),
            ],
            ["pc-gaming"] =
            [
                ("SteelSeries Rival 3 Wireless", "Chuột gaming không dây đa nền tảng cho PC, pin 400+ giờ, cảm biến TrueMove Air 18K DPI. Trọng lượng 96g, giá tốt cho game thủ PC.", 1290000, 40, "SteelSeries", "/images/uploads/razer-viper-v3-hyperspeed.webp", [imgA, imgB], ShippingClass.Nho,
                    [("DPI", "18000"), ("Kết nối", "2.4GHz + Bluetooth"), ("Pin", "400+ giờ"), ("Trọng lượng", "96g")]),
                ("HyperX Alloy Origins Core TKL", "Bàn phím cơ TKL cho PC gaming với switch HyperX Red, khung nhôm nguyên khối, đèn LED RGB per-key. Compact tiết kiệm không gian bàn.", 2290000, 22, "HyperX", "/images/uploads/corsair-k70-rgb-pro.webp", [imgC, imgD], ShippingClass.Nho,
                    [("Switch", "HyperX Red"), ("Layout", "TKL 87 phím"), ("Khung", "Nhôm"), ("Kết nối", "USB có dây")]),
                ("Creative Pebble V3 USB Speakers", "Loa USB-C 2.0 cho PC gaming và làm việc, công suất 8W, thiết kế góc 45° hướng âm thanh về phía người nghe.", 690000, 35, "Creative", "/images/uploads/logitech-g733.png", [imgA, imgD], ShippingClass.Nho,
                    [("Công suất", "8W RMS"), ("Kết nối", "USB-C"), ("Driver", "2.25 inch"), ("Điều khiển", "Nút volume tích hợp")]),
            ],
            ["console"] =
            [
                ("Nintendo Switch Pro Controller", "Tay cầm chính hãng Nintendo cho Switch và PC. HD Rumble, nút capture, pin 40 giờ, cảm giác cầm chắc tay cho game AAA.", 1790000, 25, "Nintendo", "/images/uploads/sony-dualsense.png", [imgA, imgB], ShippingClass.Nho,
                    [("Kết nối", "Bluetooth / USB-C"), ("Tương thích", "Switch / PC"), ("Pin", "40 giờ"), ("HD Rumble", "Có")]),
                ("Razer Wolverine V2 Chroma", "Tay cầm Xbox/PC có dây với 6 nút lập trình, trigger stop, đèn Chroma RGB. Switch cơ học D-pad 8 hướng chính xác.", 2490000, 18, "Razer", "/images/uploads/xbox-wireless-controller.jpg", [imgC, imgD], ShippingClass.Nho,
                    [("Kết nối", "USB có dây"), ("Tương thích", "Xbox Series X|S / PC"), ("Nút lập trình", "6 nút"), ("RGB", "Chroma")]),
                ("8BitDo Ultimate 2C Wireless", "Tay cầm không dây 2.4G cho PC và Android với Hall Effect stick chống drift, 2 nút macro sau, pin 15 giờ.", 990000, 30, "8BitDo", "/images/uploads/sony-dualsense.png", [imgB, imgD], ShippingClass.Nho,
                    [("Kết nối", "2.4G + Bluetooth"), ("Stick", "Hall Effect"), ("Pin", "15 giờ"), ("Tương thích", "PC / Android")]),
            ],
            ["mobile-gaming"] =
            [
                ("Razer Kishi V2 Pro", "Tay cầm gắn điện thoại USB-C với nút analog Hall Effect, passthrough sạc, tương thích iPhone 15 và Android. Thu gọn bỏ túi.", 2990000, 20, "Razer", "/images/uploads/xbox-wireless-controller.jpg", [imgA, imgC], ShippingClass.Nho,
                    [("Kết nối", "USB-C"), ("Stick", "Hall Effect analog"), ("Passthrough", "Sạc 60W"), ("Tương thích", "iPhone 15 / Android")]),
                ("GameSir X2 Pro Type-C", "Tay cầm mobile Type-C với trigger LT/RT, nút lập trình, thiết kế gập gọn. Hỗ trợ Xbox Cloud Gaming và GeForce NOW.", 1490000, 28, "GameSir", "/images/uploads/sony-dualsense.png", [imgB, imgD], ShippingClass.Nho,
                    [("Kết nối", "USB-C"), ("Trigger", "LT / RT analog"), ("Cloud Gaming", "Xbox / GeForce NOW"), ("Gập gọn", "Có")]),
                ("Razer Raiju Mobile", "Tay cầm Bluetooth cho mobile gaming với grip ergonomic, pin 20 giờ, tương thích game MOBA và battle royale.", 1990000, 15, "Razer", "/images/uploads/asus-rog-gladius3-wireless.webp", [imgA, imgD], ShippingClass.Nho,
                    [("Kết nối", "Bluetooth 5.0"), ("Pin", "20 giờ"), ("Tương thích", "iOS / Android"), ("Grip", "Ergonomic")]),
            ],
            ["pink-cyber"] =
            [
                ("Razer BlackWidow V3 Mini Quartz", "Bàn phím cơ 65% phiên bản Quartz hồng pastel, switch Razer Green clicky, đèn Chroma RGB. Phong cách Pink Cyber cho góc máy.", 3490000, 12, "Razer", "/images/uploads/keychron-q1-pro.jpg", [imgA, imgB], ShippingClass.Nho,
                    [("Màu", "Quartz Pink"), ("Layout", "65%"), ("Switch", "Razer Green"), ("RGB", "Chroma")]),
                ("HyperX Cloud II Pink Edition", "Tai nghe gaming màu hồng với driver 53mm, micro detachable, đệm memory foam êm ái. Phong cách cute cho streamer nữ.", 2190000, 18, "HyperX", "/images/uploads/hyperx-cloud2-wireless.jpg", [imgC, imgD], ShippingClass.Nho,
                    [("Màu", "Hồng pastel"), ("Driver", "53mm"), ("Micro", "Detachable"), ("Kết nối", "3.5mm + USB")]),
                ("Logitech G733 Lilac", "Tai nghe không dây LIGHTSPEED màu tím lilac siêu nhẹ 278g, RGB LIGHTSYNC, pin 29 giờ. Hoàn hảo cho setup Pink Cyber.", 2790000, 16, "Logitech", "/images/uploads/logitech-g733.png", [imgA, imgD], ShippingClass.Nho,
                    [("Màu", "Lilac tím"), ("Trọng lượng", "278g"), ("Kết nối", "LIGHTSPEED 2.4GHz"), ("Pin", "29 giờ")]),
            ],
            ["total-black"] =
            [
                ("Logitech G915 TKL Lightspeed Black", "Bàn phím cơ không dây low-profile TKL màu đen, switch GL Tactile, pin 40 giờ, đèn LIGHTSYNC RGB. Phong cách Total Black tối giản.", 5490000, 10, "Logitech", "/images/uploads/razer-huntsman-v3-pro.webp", [imgA, imgB], ShippingClass.Nho,
                    [("Màu", "Đen matte"), ("Layout", "TKL"), ("Switch", "GL Tactile low-profile"), ("Kết nối", "LIGHTSPEED + Bluetooth")]),
                ("Razer Kraken V3 Pro Black", "Tai nghe gaming không dây đen với THX Spatial Audio, driver TriForce Titanium 50mm, haptic feedback rung động. Phong cách all-black.", 4490000, 14, "Razer", "/images/uploads/razer-blackshark-v2-pro.webp", [imgC, imgD], ShippingClass.Nho,
                    [("Màu", "Đen"), ("Driver", "50mm TriForce"), ("Âm thanh", "THX Spatial"), ("Haptic", "Có")]),
                ("SteelSeries Aerox 3 Wireless Onyx", "Chuột gaming không dây siêu nhẹ 66g màu đen Onyx, chống nước IP54, pin 200 giờ. Hoàn thiện setup Total Black.", 1990000, 20, "SteelSeries", "/images/uploads/logitech-gpx-superlight2.png", [imgA, imgD], ShippingClass.Nho,
                    [("Màu", "Onyx đen"), ("Trọng lượng", "66g"), ("Chống nước", "IP54"), ("Pin", "200 giờ")]),
            ],
            ["snow-white"] =
            [
                ("Keychron K3 Pro White", "Bàn phím cơ không dây low-profile màu trắng, switch Gateron low-profile, hotswap, tương thích Mac/Windows. Tone Snow White tinh khôi.", 2790000, 18, "Keychron", "/images/uploads/keychron-q1-pro.jpg", [imgA, imgC], ShippingClass.Nho,
                    [("Màu", "Trắng"), ("Layout", "75% low-profile"), ("Hotswap", "Có"), ("Kết nối", "Bluetooth / USB-C")]),
                ("SteelSeries Arctis Nova 1 White", "Tai nghe gaming có dây màu trắng với driver 40mm, micro AI-powered khử ồn, khung nhẹ AirWeave. Phong cách Snow White.", 1690000, 22, "SteelSeries", "/images/uploads/logitech-g733.png", [imgB, imgD], ShippingClass.Nho,
                    [("Màu", "Trắng"), ("Driver", "40mm"), ("Micro", "AI noise cancel"), ("Kết nối", "3.5mm + USB")]),
                ("Razer DeathAdder V3 Pro White", "Chuột gaming không dây màu trắng, cảm biến Focus Pro 30K, 63g siêu nhẹ. Hoàn thiện bộ setup Snow White đồng bộ.", 3090000, 12, "Razer", "/images/uploads/razer-deathadder-v3-pro.webp", [imgA, imgD], ShippingClass.Nho,
                    [("Màu", "Trắng"), ("DPI", "30000"), ("Trọng lượng", "63g"), ("Kết nối", "HyperSpeed Wireless")]),
            ],
            ["rgb-minimalist"] =
            [
                ("NZXT Lift RGB Compact Mouse", "Chuột gaming compact với đèn RGB tối giản ở đáy, cảm biến 16K DPI, cáp paracord siêu nhẹ. Phong cách RGB Minimalist.", 890000, 30, "NZXT", "/images/uploads/logitech-g502x-plus.png", [imgA, imgB], ShippingClass.Nho,
                    [("DPI", "16000"), ("RGB", "Underglow tối giản"), ("Trọng lượng", "75g"), ("Cáp", "Paracord")]),
                ("Cooler Master CK721 RGB TKL", "Bàn phím cơ TKL wireless với RGB per-key tinh tế, switch red linear, thiết kế viền mỏng tối giản. Bluetooth + 2.4GHz.", 2490000, 16, "Cooler Master", "/images/uploads/ducky-one3-mini.jpg", [imgC, imgD], ShippingClass.Nho,
                    [("Layout", "TKL 87 phím"), ("Switch", "Red linear"), ("RGB", "Per-key"), ("Kết nối", "2.4GHz + Bluetooth")]),
                ("Razer Base Station Chroma Mercury", "Giá treo tai nghe trắng RGB Chroma tối giản, 3 cổng USB hub. Điểm nhấn ánh sáng tinh tế cho setup RGB Minimalist.", 1690000, 20, "Razer", "/images/uploads/razer-seiren-v3-chroma.webp", [imgA, imgD], ShippingClass.Nho,
                    [("Màu", "Trắng Mercury"), ("RGB", "Chroma underglow"), ("Hub", "3x USB 3.0"), ("Thiết kế", "Minimalist")]),
            ],
            ["build-your-setup"] =
            [
                ("Combo Esport Pro: Chuột + Phím + Tai nghe", "Combo tiết kiệm gồm Logitech G Pro X Superlight 2 + Corsair K70 RGB PRO + Logitech G733. Đủ bộ cho game thủ esport chuyên nghiệp.", 8990000, 8, "Logitech", "/images/uploads/logitech-gpx-superlight2.png", ["/images/uploads/corsair-k70-rgb-pro.webp", "/images/uploads/logitech-g733.png"], ShippingClass.Vua,
                    [("Bao gồm", "Chuột + Phím + Tai nghe"), ("Tiết kiệm", "~15% so với lẻ"), ("Bảo hành", "12 tháng từng sản phẩm"), ("Phù hợp", "Esport / FPS")]),
                ("Combo Stream Starter: Mic + Webcam + Đèn", "Combo streamer gồm HyperX QuadCast S + Logitech C922 + BenQ ScreenBar Halo. Bắt đầu stream chuyên nghiệp ngay.", 7490000, 6, "HyperX", "/images/uploads/hyperx-quadcast-s.jpg", ["/images/uploads/logitech-c922.png", imgA], ShippingClass.Vua,
                    [("Bao gồm", "Mic + Webcam + Đèn"), ("Tiết kiệm", "~12% so với lẻ"), ("Phù hợp", "Twitch / YouTube"), ("Cắm là dùng", "USB plug-and-play")]),
                ("Combo Desk Complete: Ghế + Bàn + Lót", "Combo setup góc máy gồm Corsair T3 Rush + Eureka Z1-S + SteelSeries QcK XXL. Trọn bộ không gian chơi game thoải mái.", 12990000, 4, "Corsair", "/images/uploads/corsair-t3-rush.webp", ["/images/uploads/dxracer-formula-gaming-chair.png", "/images/uploads/steelseries-qck-heavy-xxl.png"], ShippingClass.CongKenh,
                    [("Bao gồm", "Ghế + Bàn + Lót chuột"), ("Tiết kiệm", "~10% so với lẻ"), ("Lắp đặt", "Hướng dẫn chi tiết"), ("Phù hợp", "Setup hoàn chỉnh")]),
            ],
            ["new-arrivals"] =
            [
                ("Keychron V3 Max Wireless", "Bàn phím cơ 75% không dây mới với switch Keychron Jupiter, gasket mount, màn hình OLED tùy chỉnh. Hàng mới về hot trend 2026.", 2890000, 15, "Keychron", "/images/uploads/keychron-q1-pro.jpg", [imgA, imgB], ShippingClass.Nho,
                    [("Layout", "75%"), ("Switch", "Keychron Jupiter"), ("OLED", "Có"), ("Kết nối", "2.4GHz + Bluetooth")]),
                ("Corsair M75 Air Wireless", "Chuột gaming không dây siêu nhẹ 60g mới ra mắt, cảm biến Marksman 26K, pin 100 giờ. Thiết kế đối xứng cho claw/fingertip.", 2490000, 20, "Corsair", "/images/uploads/logitech-gpx-superlight2.png", [imgC, imgD], ShippingClass.Nho,
                    [("DPI", "26000"), ("Trọng lượng", "60g"), ("Pin", "100 giờ"), ("Cảm biến", "Marksman 26K")]),
                ("Razer Basilisk V3 Pro", "Chuột flagship mới với cảm biến Focus Pro 30K, 13 nút lập trình, sạc Qi và HyperSpeed wireless. Hàng mới về NexusGear.", 3790000, 18, "Razer", "/images/uploads/razer-deathadder-v3-pro.webp", [imgA, imgD], ShippingClass.Nho,
                    [("DPI", "30000"), ("Nút lập trình", "13 nút"), ("Sạc", "Qi wireless"), ("Kết nối", "HyperSpeed + Bluetooth")]),
            ],
            ["hot-deals"] =
            [
                ("Logitech G203 Lightsync (Giảm 30%)", "Chuột gaming có dây giá rẻ với đèn RGB LIGHTSYNC, cảm biến 8K DPI. Deal hot cho game thủ mới bắt đầu.", 490000, 50, "Logitech", "/images/uploads/razer-viper-v3-hyperspeed.webp", [imgA, imgB], ShippingClass.Nho,
                    [("Giá gốc", "700.000đ"), ("Giảm", "30%"), ("DPI", "8000"), ("RGB", "LIGHTSYNC")]),
                ("Redragon K552 Kumara (Xả kho)", "Bàn phím cơ TKL giá siêu tốt với switch Outemu Blue, đèn LED đỏ, khung thép. Deal xả kho số lượng có hạn.", 690000, 35, "Redragon", "/images/uploads/corsair-k70-rgb-pro.webp", [imgC, imgD], ShippingClass.Nho,
                    [("Giá gốc", "990.000đ"), ("Switch", "Outemu Blue"), ("Layout", "TKL 87 phím"), ("Deal", "Xả kho")]),
                ("HyperX Cloud Stinger 2 (Săn deal)", "Tai nghe gaming giá tốt với driver 50mm, micro khử ồn, xoay 90°. Giảm sâu trong tuần săn deal.", 890000, 40, "HyperX", "/images/uploads/hyperx-cloud2-wireless.jpg", [imgA, imgD], ShippingClass.Nho,
                    [("Giá gốc", "1.290.000đ"), ("Driver", "50mm"), ("Micro", "Noise cancel"), ("Deal", "Tuần này")]),
            ],
            ["webcam-den"] =
            [
                ("Logitech StreamCam", "Webcam 1080p60 với auto-focus thông minh và góc nhìn 78°. Hỗ trợ dọc 9:16 cho TikTok/Reels, USB-C kèm tripod.", 3490000, 12, "Logitech", "/images/uploads/razer-kiyo-pro.webp", [imgA, imgB], ShippingClass.Nho,
                    [("Độ phân giải", "1080p60"), ("Auto-focus", "Có"), ("Góc nhìn", "78°"), ("Kết nối", "USB-C")]),
            ],
        };

        foreach (var (slug, seeds) in categoryProducts)
        {
            if (!catMap.TryGetValue(slug, out var catId)) continue;

            var existingCount = await context.Products.CountAsync(p => p.CategoryId == catId);
            if (existingCount >= 2) continue;

            var targetCount = existingCount == 0 ? Math.Min(3, seeds.Length) : 2;
            var needed = targetCount - existingCount;

            var added = 0;
            foreach (var seed in seeds)
            {
                if (added >= needed) break;
                if (await context.Products.AnyAsync(p => p.Name == seed.Name)) continue;
                if (!brandMap.TryGetValue(seed.Brand, out var brandId)) continue;

                var secondary = JoinImageUrls(seed.Primary, seed.Secondary);
                var product = CreateProduct(seed.Name, seed.Desc, seed.Price, seed.Stock, catId, brandId,
                    seed.Primary, secondary, seed.Specs);
                product.ShippingClass = seed.Ship;
                context.Products.Add(product);
                added++;
            }
        }

        await context.SaveChangesAsync();
    }

    private static async Task EnrichAllProductImagesAsync(ApplicationDbContext context)
    {
        var imgA = "/images/uploads/43456556-fc6e-49e4-9372-6648bf58aa6c.jpg";
        var imgB = "/images/uploads/de1ae0d0-b711-45c8-b799-1a5ef3c462cb.jpg";
        var imgC = "/images/uploads/1f29ebc4-518e-452e-be74-5b7e71806138.jpg";
        var imgD = "/images/uploads/2185541f-2e12-4707-920b-07116fae04f4.jpg";

        var imageMap = new Dictionary<string, (string Primary, string[] Secondary)>
        {
            ["Razer DeathAdder V3 Pro"]               = ("/images/uploads/razer-deathadder-v3-pro.webp", [imgA, imgB]),
            ["Logitech G Pro X Superlight 2"]         = ("/images/uploads/logitech-gpx-superlight2.png", [imgC, imgD]),
            ["Corsair K70 RGB PRO"]                   = ("/images/uploads/corsair-k70-rgb-pro.webp", [imgA, imgD]),
            ["Keychron Q1 Pro"]                       = ("/images/uploads/keychron-q1-pro.jpg", [imgB, imgC]),
            ["Razer BlackShark V2 Pro"]               = ("/images/uploads/razer-blackshark-v2-pro.webp", [imgA, imgC]),
            ["Logitech G733 Lightspeed"]              = ("/images/uploads/logitech-g733.png", [imgB, imgD]),
            ["Corsair T3 Rush"]                       = ("/images/uploads/corsair-t3-rush.webp", [imgA, imgB]),
            ["Razer Huntsman V3 Pro"]                 = ("/images/uploads/razer-huntsman-v3-pro.webp", [imgC, imgD]),
            ["SteelSeries QcK Heavy XXL"]             = ("/images/uploads/steelseries-qck-heavy-xxl.png", [imgA, imgD]),
            ["HyperX QuadCast S"]                     = ("/images/uploads/hyperx-quadcast-s.jpg", [imgB, imgC]),
            ["Razer Seiren V3 Chroma"]                = ("/images/uploads/razer-seiren-v3-chroma.webp", [imgA, imgB]),
            ["Logitech C922 Pro Stream"]              = ("/images/uploads/logitech-c922.png", [imgC, imgD]),
            ["Corsair MM350 Champion Series XL"]      = ("/images/uploads/corsair-mm350-xl.webp", [imgA, imgB]),
            ["Razer Gigantus V2 XXL"]                 = ("/images/uploads/razer-gigantus-v2-xxl.webp", [imgC, imgD]),
            ["Sony DualSense Wireless Controller"]    = ("/images/uploads/sony-dualsense.png", [imgA, imgC]),
            ["Logitech G923 TRUEFORCE Racing Wheel"]  = ("/images/uploads/logitech-g923.png", [imgB, imgD]),
            ["Secretlab Titan Evo 2022"]              = ("/images/uploads/secretlab-titan-evo-2022.jpg", [imgA, imgB]),
            ["ASUS ROG Gladius III Wireless"]         = ("/images/uploads/asus-rog-gladius3-wireless.webp", [imgC, imgD]),
            ["Xbox Wireless Controller"]              = ("/images/uploads/xbox-wireless-controller.jpg", [imgA, imgD]),
            ["Corsair Virtuoso RGB Wireless XT"]      = ("/images/uploads/hyperx-cloud2-wireless.jpg", [imgB, imgC]),
            ["Blue Yeti X USB Microphone"]            = ("/images/uploads/blue-yeti-x.png", [imgA, imgD]),
            ["Razer Kiyo Pro Streaming Webcam"]       = ("/images/uploads/razer-kiyo-pro.webp", [imgB, imgC]),
            ["DXRacer Formula Series F11"]            = ("/images/uploads/dxracer-formula-gaming-chair.png", [imgA, imgC]),
            ["Ducky One 3 Mini"]                      = ("/images/uploads/ducky-one3-mini.jpg", [imgB, imgD]),
            ["SteelSeries Arctis Nova Pro Wireless"]  = ("/images/uploads/hyperx-cloud2-wireless.jpg", [imgA, imgB]),
            ["SteelSeries Apex Pro TKL Wireless"]     = ("/images/uploads/corsair-k70-rgb-pro.webp", [imgC, imgD]),
            ["Eureka Ergonomic Z1-S Gaming Desk"]     = ("/images/uploads/dxracer-formula-gaming-chair.png", [imgA, imgD]),
            ["HyperX Fury Ultra XL RGB"]              = ("/images/uploads/steelseries-qck-heavy-xxl.png", [imgB, imgC]),
            ["Logitech G502 X Plus Wireless"]         = ("/images/uploads/logitech-g502x-plus.png", [imgA, imgC]),
            ["Razer Viper V3 HyperSpeed"]             = ("/images/uploads/razer-viper-v3-hyperspeed.webp", [imgB, imgD]),
            ["HyperX Cloud II Wireless"]              = ("/images/uploads/hyperx-cloud2-wireless.jpg", [imgA, imgD]),
        };

        var products = await context.Products
            .Include(p => p.Specifications)
            .ToListAsync();

        var changed = false;
        foreach (var p in products)
        {
            if (imageMap.TryGetValue(p.Name, out var imgs))
            {
                if ((p.ImageUrl ?? "").Contains("placeholder") || p.ImageUrl != imgs.Primary)
                {
                    p.ImageUrl = imgs.Primary;
                    changed = true;
                }
                var expected = JoinImageUrls(imgs.Primary, imgs.Secondary);
                if (p.SecondaryImageUrls != expected)
                {
                    p.SecondaryImageUrls = expected;
                    changed = true;
                }
            }
            else if (!string.IsNullOrEmpty(p.ImageUrl) && !p.ImageUrl.Contains("placeholder"))
            {
                var secondaryCount = (p.SecondaryImageUrls ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).Length;
                if (secondaryCount < 2)
                {
                    p.SecondaryImageUrls = JoinImageUrls(p.ImageUrl, imgA, imgB);
                    changed = true;
                }
            }

            if (string.IsNullOrWhiteSpace(p.Description))
            {
                p.Description = $"Sản phẩm {p.Name} chính hãng tại NexusGear.";
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(p.Slug))
            {
                p.Slug = Product.GenerateSlug(p.Name);
                changed = true;
            }

            if (!p.Specifications.Any())
            {
                p.Specifications.Add(new ProductSpecification { Key = "Thương hiệu", Value = "Chính hãng" });
                p.Specifications.Add(new ProductSpecification { Key = "Bảo hành", Value = "12 tháng" });
                changed = true;
            }
        }

        if (changed)
            await context.SaveChangesAsync();
    }

    private static string JoinImageUrls(string primary, params string[] extras)
    {
        var urls = new List<string> { primary };
        foreach (var url in extras)
        {
            if (!string.IsNullOrWhiteSpace(url) && !urls.Contains(url))
                urls.Add(url);
        }
        return string.Join(",", urls);
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
            SecondaryImageUrls = secondaryImageUrls,
            Slug = Product.GenerateSlug(name)
        };
        foreach (var (key, value) in specs)
        {
            product.Specifications.Add(new ProductSpecification { Key = key, Value = value });
        }
        return product;
    }
}
