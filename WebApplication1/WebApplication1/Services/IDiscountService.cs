using System.Collections.Generic;
using WebApplication1.Models;

namespace WebApplication1.Services
{
    public interface IDiscountService
    {
        decimal ApplyDiscount(List<CartItem> cartItems, Discount discount);
    }
}