using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Dtos;

namespace WebApplication1.Controllers.Api;

[ApiController]
[Route("api/brands")]
[Produces("application/json")]
public class BrandsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public BrandsController(ApplicationDbContext db) => _db = db;

    /// <summary>Danh sách thương hiệu kèm số lượng sản phẩm đang bán</summary>
    [HttpGet]
    public async Task<ActionResult<List<BrandDto>>> GetAll()
    {
        var brands = await _db.Brands
            .OrderBy(b => b.Name)
            .Select(b => new BrandDto(
                b.Id, b.Name, b.LogoUrl,
                b.Products.Count(p => p.IsActive)))
            .ToListAsync();
        return Ok(brands);
    }

    /// <summary>Chi tiết thương hiệu</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var b = await _db.Brands.FindAsync(id);
        if (b == null) return NotFound(new { message = "Thương hiệu không tồn tại." });

        var count = await _db.Products.CountAsync(p => p.BrandId == id && p.IsActive);
        return Ok(new BrandDto(b.Id, b.Name, b.LogoUrl, count));
    }
}
