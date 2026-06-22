using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Dtos;
using WebApplication1.Models;

namespace WebApplication1.Controllers.Api;

[ApiController]
[Route("api/wishlist")]
[Produces("application/json")]
[Authorize]
public class WishlistApiController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public WishlistApiController(ApplicationDbContext db) => _db = db;
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    /// <summary>Danh sách yêu thích của người dùng</summary>
    [HttpGet]
    public async Task<ActionResult<List<WishlistItemDto>>> GetWishlist()
    {
        var items = await (
            from w in _db.WishlistItems
            where w.UserId == UserId
            join p in _db.Products on w.ProductId equals p.Id
            orderby w.CreatedAt descending
            select new WishlistItemDto(
                p.Id, p.Name, p.Price, p.ImageUrl, p.Stock > 0, w.CreatedAt)
        ).ToListAsync();

        return Ok(items);
    }

    /// <summary>Thêm sản phẩm vào danh sách yêu thích</summary>
    [HttpPost("{productId:int}")]
    public async Task<IActionResult> Add(int productId)
    {
        if (!await _db.Products.AnyAsync(p => p.Id == productId))
            return NotFound(new { message = "Sản phẩm không tồn tại." });

        if (await _db.WishlistItems.AnyAsync(w => w.UserId == UserId && w.ProductId == productId))
            return Conflict(new { message = "Sản phẩm đã có trong danh sách yêu thích." });

        _db.WishlistItems.Add(new WishlistItem
        {
            UserId    = UserId,
            ProductId = productId
        });
        await _db.SaveChangesAsync();
        return Created("/api/wishlist", new { message = "Đã thêm vào yêu thích." });
    }

    /// <summary>Xóa sản phẩm khỏi danh sách yêu thích</summary>
    [HttpDelete("{productId:int}")]
    public async Task<IActionResult> Remove(int productId)
    {
        var item = await _db.WishlistItems
            .FirstOrDefaultAsync(w => w.UserId == UserId && w.ProductId == productId);
        if (item == null) return NotFound(new { message = "Sản phẩm không có trong danh sách yêu thích." });

        _db.WishlistItems.Remove(item);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Đã xóa khỏi yêu thích." });
    }

    /// <summary>Kiểm tra sản phẩm có trong danh sách yêu thích không</summary>
    [HttpGet("{productId:int}")]
    public async Task<IActionResult> Check(int productId)
    {
        var exists = await _db.WishlistItems
            .AnyAsync(w => w.UserId == UserId && w.ProductId == productId);
        return Ok(new { productId, inWishlist = exists });
    }
}
