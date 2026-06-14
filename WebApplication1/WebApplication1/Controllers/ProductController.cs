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
        return View(vm);
    }

    public async Task<IActionResult> Details(int id)
    {
        var product = await _products.GetWithDetailsAsync(id);
        if (product == null)
            return NotFound();
        return View(product);
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

        return View(active);
    }
}
