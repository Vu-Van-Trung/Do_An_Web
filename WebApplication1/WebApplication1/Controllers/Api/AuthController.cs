using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Dtos;
using WebApplication1.Models;

namespace WebApplication1.Controllers.Api;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly SignInManager<ApplicationUser> _signIn;
    private readonly UserManager<ApplicationUser>   _users;

    public AuthController(SignInManager<ApplicationUser> signIn, UserManager<ApplicationUser> users)
    {
        _signIn = signIn;
        _users  = users;
    }

    /// <summary>Đăng nhập — trả về thông tin người dùng và thiết lập cookie phiên</summary>
    [HttpPost("login")]
    public async Task<ActionResult<AuthUserDto>> Login(LoginRequest req)
    {
        var user = await _users.FindByEmailAsync(req.Email)
                ?? await _users.Users.FirstOrDefaultAsync(u => u.PhoneNumber == req.Email);

        if (user == null)
            return Unauthorized(new { message = "Email/SĐT hoặc mật khẩu không đúng." });

        var result = await _signIn.PasswordSignInAsync(
            user, req.Password, req.RememberMe, lockoutOnFailure: true);

        if (result.IsLockedOut)
            return StatusCode(429, new { message = "Tài khoản đã bị khóa tạm thời. Vui lòng liên hệ hỗ trợ." });

        if (!result.Succeeded)
            return Unauthorized(new { message = "Email/SĐT hoặc mật khẩu không đúng." });

        var roles = await _users.GetRolesAsync(user);
        return Ok(new AuthUserDto(
            user.Id, user.FullName, user.Email!, user.PhoneNumber, user.Address, roles));
    }

    /// <summary>Đăng ký tài khoản mới</summary>
    [HttpPost("register")]
    public async Task<ActionResult<AuthUserDto>> Register(RegisterRequest req)
    {
        if (await _users.FindByEmailAsync(req.Email) != null)
            return Conflict(new { message = "Email này đã được sử dụng." });

        var user = new ApplicationUser
        {
            UserName    = req.Email,
            Email       = req.Email,
            FullName    = req.FullName,
            PhoneNumber = req.PhoneNumber
        };

        var result = await _users.CreateAsync(user, req.Password);
        if (!result.Succeeded)
            return BadRequest(new
            {
                message = "Đăng ký thất bại.",
                errors  = result.Errors.Select(e => e.Description)
            });

        await _users.AddToRoleAsync(user, "Customer");
        await _signIn.SignInAsync(user, isPersistent: false);

        return Created("/api/auth/me", new AuthUserDto(
            user.Id, user.FullName, user.Email!, user.PhoneNumber, user.Address,
            new List<string> { "Customer" }));
    }

    /// <summary>Thông tin người dùng hiện tại</summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<AuthUserDto>> Me()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var user = await _users.FindByIdAsync(userId);
        if (user == null) return Unauthorized();

        var roles = await _users.GetRolesAsync(user);
        return Ok(new AuthUserDto(
            user.Id, user.FullName, user.Email!, user.PhoneNumber, user.Address, roles));
    }

    /// <summary>Đăng xuất</summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await _signIn.SignOutAsync();
        return Ok(new { message = "Đã đăng xuất thành công." });
    }

    /// <summary>Cập nhật thông tin cá nhân</summary>
    [HttpPut("me")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest req)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var user   = await _users.FindByIdAsync(userId);
        if (user == null) return Unauthorized();

        user.FullName    = req.FullName;
        user.PhoneNumber = req.PhoneNumber;
        user.Address     = req.Address;

        var result = await _users.UpdateAsync(user);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        return Ok(new { message = "Đã cập nhật thông tin." });
    }
}

public record UpdateProfileRequest(string FullName, string PhoneNumber, string? Address);
