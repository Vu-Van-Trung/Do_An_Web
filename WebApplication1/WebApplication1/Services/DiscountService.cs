using System;
using System.Collections.Generic;
using System.Linq;
using WebApplication1.Models;

namespace WebApplication1.Services
{
    public class DiscountService : IDiscountService
    {
        public decimal ApplyDiscount(List<CartItem> cartItems, Discount discount)
        {
            // 1. Kiểm tra điều kiện cơ bản và tính tổng tiền giỏ hàng hiện tại
            if (discount == null || cartItems == null || !cartItems.Any()) return 0;

            decimal totalCartAmount = cartItems.Sum(item => item.Product.Price * item.Quantity);
            if (totalCartAmount < discount.MinOrderAmount) return 0; // Chưa đạt giá trị đơn hàng tối thiểu

            // 2. Kiểm tra điều kiện lọc theo Category
            var applicableItems = cartItems;
            if (discount.ApplicableCategoryId.HasValue)
            {
                applicableItems = cartItems
                    .Where(item => item.Product.CategoryId == discount.ApplicableCategoryId.Value)
                    .ToList();
            }

            if (!applicableItems.Any()) return 0; // Không có sản phẩm nào thuộc danh mục áp dụng

            // 3. Xử lý logic dựa trên Loại khuyến mãi (PromotionType)
            switch (discount.PromotionType)
            {
                case PromotionType.BuyXGetY:
                    // Giả định: Trường discount.Value lưu số lượng X cần mua (Ví dụ: thiết lập Value = 2 tức là mua 2 tặng 1)
                    // Nếu hệ thống chưa lưu X vào DB, mặc định lấy điều kiện là mua từ 2 sản phẩm cùng loại trở lên.
                    int x = discount.Value > 0 ? (int)discount.Value : 2;
                    int totalFreeItems = 0;

                    // Duyệt qua từng sản phẩm trong nhóm được áp dụng để tính số lượng được tặng
                    foreach (var item in applicableItems)
                    {
                        if (item.Quantity >= x)
                        {
                            totalFreeItems += (item.Quantity / x);
                        }
                    }

                    if (totalFreeItems == 0) return 0;

                    // Tìm sản phẩm có giá rẻ nhất trong số các sản phẩm hợp lệ để miễn phí/giảm giá cho món đó
                    var cheapestItem = applicableItems.OrderBy(i => i.Product.Price).First();
                    return totalFreeItems * cheapestItem.Product.Price;

                case PromotionType.Coupon:
                case PromotionType.FlashSale:
                case PromotionType.FirstOrder:
                    // Xử lý các loại giảm giá tính theo % hoặc số tiền cố định (DiscountType)
                    if (discount.DiscountType == DiscountType.Percent)
                    {
                        decimal rawDiscount = applicableItems.Sum(i => i.Product.Price * i.Quantity) * (discount.Value / 100);

                        // Kiểm tra nếu có giới hạn số tiền giảm tối đa (MaxDiscountAmount)
                        if (discount.MaxDiscountAmount.HasValue && rawDiscount > discount.MaxDiscountAmount.Value)
                        {
                            return discount.MaxDiscountAmount.Value;
                        }
                        return rawDiscount;
                    }
                    else if (discount.DiscountType == DiscountType.Fixed)
                    {
                        // Giảm thẳng một số tiền cố định, nhưng không được vượt quá tổng tiền của các sản phẩm hợp lệ
                        decimal applicableSum = applicableItems.Sum(i => i.Product.Price * i.Quantity);
                        return Math.Min(discount.Value, applicableSum);
                    }
                    break;

                case PromotionType.FreeShipping:
                    // Loại này thường được xử lý ở logic tính phí ship tại Đơn hàng, trả về 0 ở đây
                    return 0;
            }

            return 0;
        }
    }
}