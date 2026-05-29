using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Repositories;
using WebApplication1.ViewModels;

namespace WebApplication1.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class HomeController : Controller
{
    private readonly IOrderRepository _orders;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<Models.ApplicationUser> _userManager;

    public HomeController(IOrderRepository orders, ApplicationDbContext context, UserManager<Models.ApplicationUser> userManager)
    {
        _orders = orders;
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var customers = await _userManager.GetUsersInRoleAsync("Customer");
        var vm = new AdminDashboardViewModel
        {
            TotalRevenue = await _orders.GetTotalRevenueAsync(),
            OrderCount = await _orders.GetOrderCountAsync(),
            CustomerCount = customers.Count,
            ProductCount = await _context.Products.CountAsync(),
            LowStockProducts = await _context.Products.Where(p => p.Stock <= 5).OrderBy(p => p.Stock).Take(5).ToListAsync()
        };
        return View(vm);
    }
}
