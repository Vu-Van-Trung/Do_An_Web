using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models;
using WebApplication1.ViewModels;

namespace WebApplication1.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class RolesController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public RolesController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<IActionResult> Index()
    {
        var roles = new[] { "Admin", "Manager", "Staff", "Customer" };
        var usersByRole = new Dictionary<string, IList<ApplicationUser>>();
        foreach (var role in roles)
        {
            var users = await _userManager.GetUsersInRoleAsync(role);
            usersByRole[role] = users;
        }

        var vm = new RoleManagementViewModel
        {
            UsersByRole = usersByRole,
            Permissions = BuildPermissionMatrix()
        };
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignRole(string userId, string newRole)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound();

        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);
        await _userManager.AddToRoleAsync(user, newRole);

        TempData["Success"] = $"Đã gán vai trò {newRole} cho {user.Email}.";
        return RedirectToAction(nameof(Index));
    }

    private static List<PermissionRow> BuildPermissionMatrix() => new()
    {
        new("Xem Dashboard",          admin: true,  manager: true,  staff: false, customer: false),
        new("Quản lý sản phẩm",       admin: true,  manager: true,  staff: false, customer: false),
        new("Quản lý danh mục",        admin: true,  manager: true,  staff: false, customer: false),
        new("Quản lý thương hiệu",     admin: true,  manager: true,  staff: false, customer: false),
        new("Xem đơn hàng",           admin: true,  manager: true,  staff: true,  customer: false),
        new("Cập nhật trạng thái đơn", admin: true,  manager: true,  staff: true,  customer: false),
        new("Quản lý khuyến mãi",     admin: true,  manager: true,  staff: false, customer: false),
        new("Xem báo cáo doanh thu",  admin: true,  manager: true,  staff: false, customer: false),
        new("Quản lý người dùng",     admin: true,  manager: false, staff: false, customer: false),
        new("Phân quyền vai trò",     admin: true,  manager: false, staff: false, customer: false),
        new("Đặt hàng",               admin: false, manager: false, staff: false, customer: true),
        new("Xem đơn hàng của mình",  admin: false, manager: false, staff: false, customer: true),
    };
}
