namespace WebApplication1.Models;

/// <summary>Kiểu giá trị giảm: phần trăm hoặc số tiền cố định</summary>
public enum DiscountType { Percent, Fixed }

/// <summary>Loại khuyến mãi đa dạng</summary>
public enum PromotionType
{
    Coupon,       // Mã giảm giá thông thường (% hoặc cố định)
    FlashSale,    // Deal giới hạn thời gian
    FreeShipping, // Miễn phí vận chuyển
    FirstOrder,   // Ưu đãi đơn hàng đầu tiên
    BuyXGetY      // Mua X tặng/giảm Y (tương lai)
}

public class Discount
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    // Loại khuyến mãi (mới)
    public PromotionType PromotionType { get; set; } = PromotionType.Coupon;

    // Kiểu & giá trị giảm (dùng cho Coupon / FlashSale / FirstOrder)
    public DiscountType DiscountType { get; set; } = DiscountType.Percent;
    public decimal Value { get; set; }

    /// <summary>Giảm tối đa tính theo ₫ (chỉ áp dụng khi DiscountType = Percent)</summary>
    public decimal? MaxDiscountAmount { get; set; }

    public decimal MinOrderAmount { get; set; }
    public int? MaxUsage { get; set; }
    public int UsedCount { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; } = true;

    // Giới hạn theo category (tuỳ chọn)
    public int? ApplicableCategoryId { get; set; }
}
