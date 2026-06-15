using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Controllers;

[Authorize]
public class WishlistController : Controller
{
    private readonly ApplicationDbContext _context;

    public WishlistController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var items = await _context.WishlistItems
            .Include(w => w.Product)
            .ThenInclude(p => p.Brand)
            .Include(w => w.Product)
            .ThenInclude(p => p.Category)
            .Where(w => w.UserId == userId)
            .OrderByDescending(w => w.CreatedAt)
            .ToListAsync();

        return View(items);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int productId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var productExists = await _context.Products.AnyAsync(p => p.Id == productId);
        if (!productExists) return NotFound();

        var exists = await _context.WishlistItems.AnyAsync(w => w.UserId == userId && w.ProductId == productId);
        if (!exists)
        {
            var item = new WishlistItem
            {
                UserId = userId,
                ProductId = productId,
                CreatedAt = DateTime.UtcNow
            };
            _context.WishlistItems.Add(item);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã thêm sản phẩm vào danh sách yêu thích.";
        }

        var referer = Request.Headers["Referer"].ToString();
        return string.IsNullOrEmpty(referer) ? RedirectToAction("Index") : Redirect(referer);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int productId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var item = await _context.WishlistItems.FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);
        if (item != null)
        {
            _context.WishlistItems.Remove(item);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã xóa sản phẩm khỏi danh sách yêu thích.";
        }

        var referer = Request.Headers["Referer"].ToString();
        return string.IsNullOrEmpty(referer) ? RedirectToAction("Index") : Redirect(referer);
    }
}
