using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.Repositories;
using WebApplication1.ViewModels;

namespace WebApplication1.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,Manager")]
public class HomeController : Controller
{
    private readonly IOrderRepository _orders;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public HomeController(IOrderRepository orders, ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _orders = orders;
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var now = DateTime.UtcNow;
        var customers = await _userManager.GetUsersInRoleAsync("Customer");

        var allOrders = await _context.Orders
            .Include(o => o.User)
            .Include(o => o.Items)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

        var monthlyRevenues = Enumerable.Range(0, 6)
            .Select(i => now.AddMonths(-i))
            .Select(m => new MonthlyRevenue
            {
                Month = m.ToString("MM/yyyy"),
                Revenue = allOrders
                    .Where(o => o.Status != OrderStatus.Cancelled
                                && o.OrderDate.Year == m.Year
                                && o.OrderDate.Month == m.Month)
                    .Sum(o => o.Total)
            })
            .Reverse()
            .ToList();

        var vm = new AdminDashboardViewModel
        {
            TotalRevenue = allOrders.Where(o => o.Status != OrderStatus.Cancelled).Sum(o => o.Total),
            OrderCount = allOrders.Count,
            PendingOrderCount = allOrders.Count(o => o.Status == OrderStatus.Pending),
            CustomerCount = customers.Count,
            ProductCount = await _context.Products.CountAsync(),
            LowStockProducts = await _context.Products.Where(p => p.Stock <= 5).OrderBy(p => p.Stock).Take(5).ToListAsync(),
            RecentOrders = allOrders.Take(8),
            MonthlyRevenues = monthlyRevenues
        };

        return View(vm);
    }
}
