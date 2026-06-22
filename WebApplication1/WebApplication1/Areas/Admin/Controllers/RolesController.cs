using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models;
using WebApplication1.ViewModels;

namespace WebApplication1.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "PhanQuyenVaiTro")]
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

        var adminClaims = await GetRolePermissionsAsync("Admin");
        var managerClaims = await GetRolePermissionsAsync("Manager");
        var staffClaims = await GetRolePermissionsAsync("Staff");
        var customerClaims = await GetRolePermissionsAsync("Customer");

        var permissions = new List<PermissionRow>();
        foreach (var entry in AppPermissions.DisplayNames)
        {
            permissions.Add(new PermissionRow(
                entry.Value,
                entry.Key,
                admin: adminClaims.Contains(entry.Key),
                manager: managerClaims.Contains(entry.Key),
                staff: staffClaims.Contains(entry.Key),
                customer: customerClaims.Contains(entry.Key)
            ));
        }

        var vm = new RoleManagementViewModel
        {
            UsersByRole = usersByRole,
            Permissions = permissions
        };
        return View(vm);
    }

    private async Task<HashSet<string>> GetRolePermissionsAsync(string roleName)
    {
        var role = await _roleManager.FindByNameAsync(roleName);
        if (role == null) return new HashSet<string>();
        var claims = await _roleManager.GetClaimsAsync(role);
        return claims
            .Where(c => c.Type == "Permission")
            .Select(c => c.Value)
            .ToHashSet();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdatePermissions(Dictionary<string, List<string>> rolePermissions)
    {
        var roles = new[] { "Admin", "Manager", "Staff", "Customer" };
        foreach (var roleName in roles)
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role == null) continue;

            var existingClaims = await _roleManager.GetClaimsAsync(role);
            var permissionClaims = existingClaims.Where(c => c.Type == "Permission").ToList();

            var submittedPerms = rolePermissions != null && rolePermissions.ContainsKey(roleName)
                ? rolePermissions[roleName]
                : new List<string>();

            // Remove unchecked claims
            foreach (var claim in permissionClaims)
            {
                if (!submittedPerms.Contains(claim.Value))
                {
                    await _roleManager.RemoveClaimAsync(role, claim);
                }
            }

            // Add newly checked claims
            var existingPermKeys = permissionClaims.Select(c => c.Value).ToHashSet();
            foreach (var perm in submittedPerms)
            {
                if (AppPermissions.DisplayNames.ContainsKey(perm) && !existingPermKeys.Contains(perm))
                {
                    await _roleManager.AddClaimAsync(role, new System.Security.Claims.Claim("Permission", perm));
                }
            }
        }

        // Invalidate current user's security stamp to force claim refresh on next request
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser != null)
        {
            await _userManager.UpdateSecurityStampAsync(currentUser);
        }

        TempData["Success"] = "Đã cập nhật ma trận quyền hạn thành công.";
        return RedirectToAction(nameof(Index));
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
}
