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

        // Calculate average rating dynamically
        double avgRating = 4.9;
        if (await _context.ProductReviews.AnyAsync())
        {
            avgRating = await _context.ProductReviews.AverageAsync(r => r.Rating);
        }
        ViewBag.AverageRating = avgRating.ToString("F1");

        return View(featured);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
