using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models;
using WebApplication1.Repositories;

namespace WebApplication1.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,Manager,Staff")]
public class OrdersController : Controller
{
    private readonly IOrderRepository _orders;

    public OrdersController(IOrderRepository orders)
    {
        _orders = orders;
    }

    public async Task<IActionResult> Index() =>
        View(await _orders.GetAllWithItemsAsync());

    public async Task<IActionResult> Details(int id)
    {
        var order = await _orders.GetWithItemsAsync(id);
        if (order == null) return NotFound();
        return View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, OrderStatus status)
    {
        var order = await _orders.GetByIdAsync(id);
        if (order == null) return NotFound();
        order.Status = status;
        _orders.Update(order);
        await _orders.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id });
    }
}
