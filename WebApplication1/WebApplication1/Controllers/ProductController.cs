using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.Repositories;
using WebApplication1.ViewModels;

namespace WebApplication1.Controllers;

public class ProductController : Controller
{
    private readonly IProductRepository _products;
    private readonly ApplicationDbContext _context;
    private readonly IDiscountRepository _discounts;

    public ProductController(IProductRepository products, ApplicationDbContext context, IDiscountRepository discounts)
    {
        _products  = products;
        _context   = context;
        _discounts = discounts;
    }

    public async Task<IActionResult> Index(ProductFilter filter)
    {
        var vm = new CatalogViewModel
        {
            Filter      = filter,
            Products    = await _products.GetFilteredAsync(filter),
            Categories  = await _context.Categories.OrderBy(c => c.Name).ToListAsync(),
            Brands      = await _context.Brands.OrderBy(b => b.Name).ToListAsync(),
            Connections = await _products.GetSpecValuesAsync("Connection"),
            SwitchTypes = await _products.GetSpecValuesAsync("Switch Type"),
            DpiOptions  = await _products.GetSpecValuesAsync("DPI")
        };
        var now = DateTime.UtcNow;
        List<Discount> activeDiscounts = new();

        if (User.Identity?.IsAuthenticated == true)
        {
            var userId = _context.Users.Where(u => u.UserName == User.Identity.Name).Select(u => u.Id).FirstOrDefault();
            if (!string.IsNullOrEmpty(userId))
            {
                var orderCount = await _context.Orders.CountAsync(o => o.UserId == userId);
                var discountsQuery = _context.Discounts
                    .Where(d => d.IsActive && d.StartDate <= now && d.EndDate >= now
                                && (d.MaxUsage == null || d.UsedCount < d.MaxUsage));

                if (orderCount > 0)
                {
                    discountsQuery = discountsQuery.Where(d => d.PromotionType != PromotionType.FirstOrder);
                }
                activeDiscounts = await discountsQuery.ToListAsync();
            }
        }

        ViewBag.ActiveDiscounts = activeDiscounts;

        return View(vm);
    }

    public async Task<IActionResult> Details(int id)
    {
        var product = await _products.GetWithDetailsAsync(id);
        if (product == null)
            return NotFound();

        var reviews = await _context.ProductReviews
            .Include(r => r.User)
            .Where(r => r.ProductId == id)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
        ViewBag.Reviews = reviews;

        double avgRating = 0;
        if (reviews.Any())
        {
            avgRating = reviews.Average(r => r.Rating);
        }
        ViewBag.AverageRating = avgRating;

        var now = DateTime.UtcNow;
        List<Discount> activeDiscounts = new();

        if (User.Identity?.IsAuthenticated == true)
        {
            var userId = _context.Users.Where(u => u.UserName == User.Identity.Name).Select(u => u.Id).FirstOrDefault();
            if (!string.IsNullOrEmpty(userId))
            {
                var orderCount = await _context.Orders.CountAsync(o => o.UserId == userId);
                var discountsQuery = _context.Discounts
                    .Where(d => d.IsActive && d.StartDate <= now && d.EndDate >= now
                                && (d.MaxUsage == null || d.UsedCount < d.MaxUsage));

                if (orderCount > 0)
                {
                    discountsQuery = discountsQuery.Where(d => d.PromotionType != PromotionType.FirstOrder);
                }
                activeDiscounts = await discountsQuery.ToListAsync();
            }
        }

        ViewBag.ActiveDiscounts = activeDiscounts;

        return View(product);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddReview(int productId, int rating, string comment)
    {
        var product = await _context.Products.FindAsync(productId);
        if (product == null) return NotFound();

        if (rating < 1 || rating > 5)
        {
            TempData["Error"] = "Số sao đánh giá phải từ 1 đến 5!";
            return RedirectToAction(nameof(Details), new { id = productId });
        }

        var userId = _context.Users.Where(u => u.UserName == User.Identity!.Name).Select(u => u.Id).FirstOrDefault();
        if (string.IsNullOrEmpty(userId)) return Challenge();

        var review = new ProductReview
        {
            ProductId = productId,
            UserId = userId,
            Rating = rating,
            Comment = comment ?? string.Empty,
            CreatedAt = DateTime.UtcNow
        };

        _context.ProductReviews.Add(review);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Cảm ơn bạn đã đánh giá sản phẩm!";
        return RedirectToAction(nameof(Details), new { id = productId });
    }

    /// <summary>Trang Khuyến mãi công khai — hiển thị tất cả deal đang hoạt động</summary>
    public async Task<IActionResult> Deals()
    {
        var now = DateTime.UtcNow;
        var all = await _discounts.GetAllAsync();

        var active = all
            .Where(d => d.IsActive && d.StartDate <= now && d.EndDate >= now
                        && (d.MaxUsage == null || d.UsedCount < d.MaxUsage))
            .OrderBy(d => d.PromotionType)
            .ThenByDescending(d => d.Value)
            .ToList();

        ViewBag.FlashSales    = active.Where(d => d.PromotionType == PromotionType.FlashSale).ToList();
        ViewBag.FreeShipping  = active.Where(d => d.PromotionType == PromotionType.FreeShipping).ToList();
        ViewBag.FirstOrders   = active.Where(d => d.PromotionType == PromotionType.FirstOrder).ToList();
        ViewBag.Coupons       = active.Where(d => d.PromotionType == PromotionType.Coupon).ToList();

        bool isNewUser = false;
        if (User.Identity?.IsAuthenticated == true)
        {
            var userId = _context.Users.Where(u => u.UserName == User.Identity!.Name).Select(u => u.Id).FirstOrDefault();
            if (!string.IsNullOrEmpty(userId))
            {
                var orderCount = await _context.Orders.CountAsync(o => o.UserId == userId);
                isNewUser = (orderCount == 0);
            }
        }
        ViewBag.IsNewUser = isNewUser;

        return View(active);
    }
}
