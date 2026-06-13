using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.ViewModels;

namespace WebApplication1.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,Manager")]
public class ReportsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public ReportsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var now = DateTime.UtcNow;
        var firstOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var orders = await _context.Orders
            .Include(o => o.Items)
            .Where(o => o.Status != OrderStatus.Cancelled)
            .ToListAsync();

        // Doanh thu 12 tháng gần nhất
        var monthlyRevenues = Enumerable.Range(0, 12)
            .Select(i => now.AddMonths(-i))
            .Select(m => new MonthlyRevenue
            {
                Month = m.ToString("MM/yyyy"),
                Revenue = orders
                    .Where(o => o.OrderDate.Year == m.Year && o.OrderDate.Month == m.Month)
                    .Sum(o => o.Total)
            })
            .Reverse()
            .ToList();

        // Top 10 sản phẩm bán chạy
        var topProducts = await _context.OrderItems
            .Include(oi => oi.Order)
            .Where(oi => oi.Order.Status != OrderStatus.Cancelled)
            .GroupBy(oi => oi.ProductName)
            .Select(g => new TopProduct
            {
                Name = g.Key,
                TotalSold = g.Sum(oi => oi.Quantity),
                TotalRevenue = g.Sum(oi => oi.UnitPrice * oi.Quantity)
            })
            .OrderByDescending(x => x.TotalSold)
            .Take(10)
            .ToListAsync();

        var allOrders = await _context.Orders.ToListAsync();
        var customers = await _userManager.GetUsersInRoleAsync("Customer");

        var vm = new ReportViewModel
        {
            MonthlyRevenues = monthlyRevenues,
            TopProducts = topProducts,
            TotalOrders = allOrders.Count,
            TotalRevenue = orders.Sum(o => o.Total),
            CompletedOrders = allOrders.Count(o => o.Status == OrderStatus.Completed),
            CancelledOrders = allOrders.Count(o => o.Status == OrderStatus.Cancelled),
            NewCustomersThisMonth = customers.Count(u => u.CreatedAt >= firstOfMonth)
        };

        return View(vm);
    }
}
