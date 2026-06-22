using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;

    public HomeController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var featured = await _context.Products
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .Where(p => p.IsActive)
            .OrderByDescending(p => p.CreatedAt)
            .Take(8)
            .ToListAsync();

        ViewBag.Brands = await _context.Brands.OrderBy(b => b.Name).ToListAsync();

        // Calculate average rating dynamically for store stats
        double avgRating = 4.9;
        if (await _context.ProductReviews.AnyAsync())
        {
            avgRating = await _context.ProductReviews.AverageAsync(r => r.Rating);
        }
        ViewBag.AverageRating = avgRating.ToString("F1");

        // Calculate rating per product
        var featuredIds = featured.Select(p => p.Id).ToList();
        var productRatings = await _context.ProductReviews
            .Where(r => featuredIds.Contains(r.ProductId))
            .GroupBy(r => r.ProductId)
            .Select(g => new { ProductId = g.Key, AverageRating = g.Average(r => r.Rating), Count = g.Count() })
            .ToDictionaryAsync(x => x.ProductId, x => (AverageRating: x.AverageRating, Count: x.Count));
        ViewBag.ProductRatings = productRatings;

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

        return View(featured);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
