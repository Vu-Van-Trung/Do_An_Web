namespace WebApplication1.Services;

// ───────────────────────────────────────────────
// ENUMS
// ───────────────────────────────────────────────

public enum ShippingRegion { NoiThanhHCM, MienNam, MienTrung, MienBac }

public enum ShippingService { TietKiem, Nhanh, HoaToc }

public enum ShippingClass
{
    Nho,        // × 1.0  — chuột, bàn phím, tai nghe nhỏ
    Vua,        // × 1.5  — tai nghe full-size, bộ loa nhỏ
    CongKenh,   // × 3.0  — ghế gaming, bàn gaming
}

// ───────────────────────────────────────────────
// RESULT DTO — bind thẳng vào View
// ───────────────────────────────────────────────

public sealed record ShippingOption(
    ShippingService Service,
    string          ServiceName,
    decimal         Fee,
    string          Eta,
    bool            IsContactOnly   // true → hiện "Liên hệ báo phí"
);

// ───────────────────────────────────────────────
// CORE CALCULATOR
// ───────────────────────────────────────────────

public static class ShippingCalculator
{
    // ── 1. Bảng giá nền (≤ 3 kg): [Region][Service] = (phí, ETA) ──────────
    private static readonly IReadOnlyDictionary<ShippingRegion,
        IReadOnlyDictionary<ShippingService, (decimal Fee, string Eta)>> BaseRates =
        new Dictionary<ShippingRegion,
            IReadOnlyDictionary<ShippingService, (decimal, string)>>
    {
        [ShippingRegion.NoiThanhHCM] = new Dictionary<ShippingService, (decimal, string)>
        {
            [ShippingService.TietKiem] = (25_000, "2–3 ngày"),
            [ShippingService.Nhanh]    = (35_000, "Hôm nay – ngày mai"),
            [ShippingService.HoaToc]   = (60_000, "2–4 giờ"),
        },
        [ShippingRegion.MienNam] = new Dictionary<ShippingService, (decimal, string)>
        {
            [ShippingService.TietKiem] = (35_000, "3–5 ngày"),
            [ShippingService.Nhanh]    = (50_000, "1–2 ngày"),
            [ShippingService.HoaToc]   = (90_000, "Trong ngày"),
        },
        [ShippingRegion.MienTrung] = new Dictionary<ShippingService, (decimal, string)>
        {
            [ShippingService.TietKiem] = (50_000, "4–6 ngày"),
            [ShippingService.Nhanh]    = (70_000, "2–3 ngày"),
            [ShippingService.HoaToc]   = (120_000, "1–2 ngày"),
        },
        [ShippingRegion.MienBac] = new Dictionary<ShippingService, (decimal, string)>
        {
            [ShippingService.TietKiem] = (70_000, "5–7 ngày"),
            [ShippingService.Nhanh]    = (95_000, "2–4 ngày"),
            [ShippingService.HoaToc]   = (150_000, "2–3 ngày"),
        },
    };

    // ── 2. Hệ số theo ShippingClass ────────────────────────────────────────
    private static readonly IReadOnlyDictionary<ShippingClass, decimal> ClassMultiplier =
        new Dictionary<ShippingClass, decimal>
        {
            [ShippingClass.Nho]      = 1.0m,
            [ShippingClass.Vua]      = 1.5m,
            [ShippingClass.CongKenh] = 3.0m,
        };

    // ── 3. Map 34 tỉnh → Region (dữ liệu từ API v2) ────────────────────────
    // code 79 = TP. Hồ Chí Minh
    private static readonly HashSet<int> HCM     = [79];

    // Các tỉnh phía Nam (không gồm HCM)
    private static readonly HashSet<int> Nam      = [75, 80, 82, 86, 91, 92, 96];
    //  75 Đồng Nai | 80 Tây Ninh | 82 Đồng Tháp | 86 Vĩnh Long
    //  91 An Giang | 92 Cần Thơ  | 96 Cà Mau

    // Miền Trung & Tây Nguyên
    private static readonly HashSet<int> Trung    = [38, 40, 42, 44, 46, 48, 51, 52, 56, 66, 68];
    //  38 Thanh Hóa | 40 Nghệ An  | 42 Hà Tĩnh   | 44 Quảng Trị | 46 Huế
    //  48 Đà Nẵng   | 51 Quảng Ngãi | 52 Gia Lai | 56 Khánh Hòa
    //  66 Đắk Lắk   | 68 Lâm Đồng

    // Miền Bắc: 1 HN | 4 Cao Bằng | 8 Tuyên Quang | 11 Điện Biên | 12 Lai Châu
    //          14 Sơn La | 15 Lào Cai | 19 Thái Nguyên | 20 Lạng Sơn | 22 Quảng Ninh
    //          24 Bắc Ninh | 25 Phú Thọ | 31 Hải Phòng | 33 Hưng Yên | 37 Ninh Bình

    public static ShippingRegion GetRegion(int provinceCode) => provinceCode switch
    {
        _ when HCM.Contains(provinceCode)   => ShippingRegion.NoiThanhHCM,
        _ when Nam.Contains(provinceCode)   => ShippingRegion.MienNam,
        _ when Trung.Contains(provinceCode) => ShippingRegion.MienTrung,
        _                                   => ShippingRegion.MienBac,
    };

    // ── 4. Hàm chính ─────────────────────────────────────────────────────────
    /// <summary>
    /// Tính danh sách option ship khả dụng để bind vào view checkout.
    /// </summary>
    /// <param name="provinceCode">Mã tỉnh từ API provinces.open-api.vn/api/v2/p/</param>
    /// <param name="shippingClass">Loại hàng (Nho / Vua / CongKenh)</param>
    public static List<ShippingOption> CalculateShippingFee(
        int           provinceCode,
        ShippingClass shippingClass)
    {
        var region     = GetRegion(provinceCode);
        var multiplier = ClassMultiplier[shippingClass];
        var isBulky    = shippingClass == ShippingClass.CongKenh;
        var isHCM      = region == ShippingRegion.NoiThanhHCM;
        var options    = new List<ShippingOption>();

        foreach (var (service, (baseFee, eta)) in BaseRates[region])
        {
            // Quy tắc: ẩn Hỏa Tốc cho hàng cồng kềnh
            if (isBulky && service == ShippingService.HoaToc)
                continue;

            if (isBulky && isHCM)
            {
                // Cồng kềnh nội thành HCM → freeship, shop tự giao
                options.Add(new ShippingOption(
                    service, GetServiceName(service),
                    Fee: 0, Eta: eta + " (shop tự giao)",
                    IsContactOnly: false));
                continue;
            }

            if (isBulky)
            {
                // Cồng kềnh liên tỉnh → liên hệ báo phí
                options.Add(new ShippingOption(
                    service, GetServiceName(service),
                    Fee: 0, Eta: eta,
                    IsContactOnly: true));
                continue;
            }

            // Làm tròn đến nghìn đồng gần nhất
            var fee = Math.Round(baseFee * multiplier / 1000m, 0) * 1000m;
            options.Add(new ShippingOption(
                service, GetServiceName(service),
                Fee: fee, Eta: eta,
                IsContactOnly: false));
        }

        return options;
    }

    // ── 5. Helper: lấy phí của một dịch vụ cụ thể (cho Controller) ──────────
    public static decimal GetFee(int provinceCode, ShippingClass shippingClass,
        ShippingService service = ShippingService.Nhanh)
    {
        var option = CalculateShippingFee(provinceCode, shippingClass)
            .FirstOrDefault(o => o.Service == service && !o.IsContactOnly);
        return option?.Fee ?? 0;
    }

    private static string GetServiceName(ShippingService s) => s switch
    {
        ShippingService.TietKiem => "Tiết kiệm",
        ShippingService.Nhanh    => "Nhanh",
        ShippingService.HoaToc   => "Hỏa tốc",
        _                        => s.ToString(),
    };
}
