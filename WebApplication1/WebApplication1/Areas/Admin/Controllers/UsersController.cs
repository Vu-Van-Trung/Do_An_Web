using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;
using WebApplication1.Repositories;
using WebApplication1.ViewModels;

namespace WebApplication1.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class UsersController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IOrderRepository _orders;

    public UsersController(UserManager<ApplicationUser> userManager, IOrderRepository orders)
    {
        _userManager = userManager;
        _orders = orders;
    }

    public async Task<IActionResult> Index()
    {
        var users = await _userManager.Users
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync();

        var result = new List<UserListViewModel>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var lockout = await _userManager.IsLockedOutAsync(user);
            var orderCount = await _orders.FindAsync(o => o.UserId == user.Id);
            result.Add(new UserListViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                CreatedAt = user.CreatedAt,
                IsLocked = lockout,
                Role = roles.FirstOrDefault() ?? "Customer",
                OrderCount = orderCount.Count()
            });
        }

        return View(result);
    }

    public async Task<IActionResult> Details(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        var roles = await _userManager.GetRolesAsync(user);
        var lockout = await _userManager.IsLockedOutAsync(user);
        var userOrders = await _orders.GetByUserAsync(id);

        return View(new UserDetailViewModel
        {
            User = user,
            Role = roles.FirstOrDefault() ?? "Customer",
            IsLocked = lockout,
            Orders = userOrders
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleLock(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        if (await _userManager.IsLockedOutAsync(user))
        {
            await _userManager.SetLockoutEndDateAsync(user, null);
            TempData["Success"] = $"Đã mở khóa tài khoản {user.Email}.";
        }
        else
        {
            await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
            TempData["Success"] = $"Đã khóa tài khoản {user.Email}.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeRole(string id, string role)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);
        await _userManager.AddToRoleAsync(user, role);

        TempData["Success"] = $"Đã thay đổi vai trò tài khoản {user.Email} thành {role}.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        if (await _userManager.IsInRoleAsync(user, "Admin"))
        {
            TempData["Error"] = "Không thể xóa tài khoản Admin.";
            return RedirectToAction(nameof(Index));
        }

        await _userManager.DeleteAsync(user);
        TempData["Success"] = "Đã xóa tài khoản.";
        return RedirectToAction(nameof(Index));
    }
}
