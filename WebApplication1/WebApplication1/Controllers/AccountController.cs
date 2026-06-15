using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models;
using WebApplication1.Services;
using WebApplication1.ViewModels;

namespace WebApplication1.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ICartService _cartService;
    private readonly Repositories.IOrderRepository _orders;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ICartService cartService,
        Repositories.IOrderRepository orders)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _cartService = cartService;
        _orders = orders;
    }

    [HttpGet]
    public IActionResult Register(string? returnUrl = null)
    {
        return View(new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName,
            PhoneNumber = model.PhoneNumber,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        await _userManager.AddToRoleAsync(user, "Customer");
        await _signInManager.SignInAsync(user, isPersistent: false);
        await _cartService.MergeSessionCartAsync(user.Id);

        return RedirectToLocal(returnUrl ?? Url.Action("Orders", "Account")!);
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _signInManager.PasswordSignInAsync(
            model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user != null)
            await _cartService.MergeSessionCartAsync(user.Id);

        return RedirectToLocal(model.ReturnUrl ?? Url.Action("Index", "Home")!);
    }

    // ── Google OAuth ──────────────────────────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ExternalLogin(string provider, string? returnUrl = null)
    {
        var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { returnUrl });
        var props = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        return Challenge(props, provider);
    }

    public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null, string? remoteError = null)
    {
        if (remoteError != null)
        {
            ModelState.AddModelError(string.Empty, $"Lỗi từ Google: {remoteError}");
            return View("Login", new LoginViewModel());
        }

        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
            return RedirectToAction(nameof(Login));

        var result = await _signInManager.ExternalLoginSignInAsync(
            info.LoginProvider, info.ProviderKey, isPersistent: false);

        if (result.Succeeded)
        {
            var u = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
            if (u != null) await _cartService.MergeSessionCartAsync(u.Id);
            return RedirectToLocal(returnUrl ?? Url.Action("Index", "Home")!);
        }

        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        var name  = info.Principal.FindFirstValue(ClaimTypes.Name);

        if (email == null)
        {
            ModelState.AddModelError(string.Empty, "Không lấy được email từ Google.");
            return View("Login", new LoginViewModel());
        }

        // Liên kết với tài khoản cũ nếu email đã tồn tại
        var existing = await _userManager.FindByEmailAsync(email);
        if (existing != null)
        {
            await _userManager.AddLoginAsync(existing, info);
            await _signInManager.SignInAsync(existing, isPersistent: false);
            await _cartService.MergeSessionCartAsync(existing.Id);
            return RedirectToLocal(returnUrl ?? Url.Action("Index", "Home")!);
        }

        // Tạo tài khoản mới từ Google
        var newUser = new ApplicationUser
        {
            UserName       = email,
            Email          = email,
            FullName       = name ?? email,
            EmailConfirmed = true
        };
        var createResult = await _userManager.CreateAsync(newUser);
        if (createResult.Succeeded)
        {
            await _userManager.AddLoginAsync(newUser, info);
            await _userManager.AddToRoleAsync(newUser, "Customer");
            await _signInManager.SignInAsync(newUser, isPersistent: false);
            await _cartService.MergeSessionCartAsync(newUser.Id);
            return RedirectToLocal(returnUrl ?? Url.Action("Index", "Home")!);
        }

        foreach (var err in createResult.Errors)
            ModelState.AddModelError(string.Empty, err.Description);
        return View("Login", new LoginViewModel());
    }

    [Authorize]
    public async Task<IActionResult> Orders()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        return View(await _orders.GetByUserAsync(userId));
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    // ── PROFILE ──────────────────────────────────────────────────────────────
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        var model = new ProfileViewModel
        {
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber,
            Address = user.Address
        };
        return View(model);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        user.FullName = model.FullName;
        user.PhoneNumber = model.PhoneNumber;
        user.Address = model.Address;

        var result = await _userManager.UpdateAsync(user);
        if (result.Succeeded)
        {
            TempData["Success"] = "Cập nhật thông tin cá nhân thành công!";
            return RedirectToAction(nameof(Profile));
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
        return View(model);
    }

    // ── CHANGE PASSWORD ──────────────────────────────────────────────────────
    [Authorize]
    [HttpGet]
    public IActionResult ChangePassword()
    {
        return View(new ChangePasswordViewModel());
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        var result = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
        if (result.Succeeded)
        {
            await _signInManager.RefreshSignInAsync(user);
            TempData["Success"] = "Đổi mật khẩu thành công!";
            return RedirectToAction(nameof(ChangePassword));
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
        return View(model);
    }

    // ── FORGOT PASSWORD ──────────────────────────────────────────────────────
    [HttpGet]
    public IActionResult ForgotPassword()
    {
        return View(new ForgotPasswordViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            // Don't reveal that the user does not exist
            return RedirectToAction(nameof(ForgotPasswordConfirmation));
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var callbackUrl = Url.Action("ResetPassword", "Account", new { token, email = model.Email }, Request.Scheme);
        
        // Save the direct reset link to TempData so it can be displayed on the screen for local testing!
        TempData["DirectResetLink"] = callbackUrl;

        return RedirectToAction(nameof(ForgotPasswordConfirmation));
    }

    [HttpGet]
    public IActionResult ForgotPasswordConfirmation()
    {
        return View();
    }

    // ── RESET PASSWORD ────────────────────────────────────────────────────────
    [HttpGet]
    public IActionResult ResetPassword(string? token = null, string? email = null)
    {
        if (token == null || email == null)
        {
            return BadRequest("Token và Email không hợp lệ để reset mật khẩu.");
        }
        return View(new ResetPasswordViewModel { Token = token, Email = email });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            // Don't reveal that the user does not exist
            return RedirectToAction(nameof(ResetPasswordConfirmation));
        }

        var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);
        if (result.Succeeded)
        {
            return RedirectToAction(nameof(ResetPasswordConfirmation));
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
        return View(model);
    }

    // ── ORDER DETAILS & CANCELLATION ─────────────────────────────────────────
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> OrderDetails(int id)
    {
        var order = await _orders.GetWithItemsAsync(id);
        if (order == null) return NotFound();

        var userId = _userManager.GetUserId(User);
        if (order.UserId != userId && !User.IsInRole("Admin"))
        {
            return Forbid();
        }

        return View(order);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelOrder(int id)
    {
        var order = await _orders.GetByIdAsync(id);
        if (order == null) return NotFound();

        var userId = _userManager.GetUserId(User);
        if (order.UserId != userId)
        {
            return Forbid();
        }

        if (order.Status != OrderStatus.Pending)
        {
            TempData["Error"] = "Chỉ có thể hủy đơn hàng ở trạng thái Chờ xử lý!";
            return RedirectToAction(nameof(OrderDetails), new { id });
        }

        order.Status = OrderStatus.Cancelled;
        _orders.Update(order);
        await _orders.SaveChangesAsync();

        TempData["Success"] = "Đơn hàng đã được hủy thành công.";
        return RedirectToAction(nameof(OrderDetails), new { id });
    }

    [HttpGet]
    public IActionResult ResetPasswordConfirmation()
    {
        return View();
    }

    public IActionResult AccessDenied() => View();

    private IActionResult RedirectToLocal(string returnUrl)
    {
        if (Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);
        return RedirectToAction("Index", "Home");
    }
}
