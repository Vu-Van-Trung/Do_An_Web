using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Dtos;

namespace WebApplication1.Controllers.Api;

[ApiController]
[Route("api/categories")]
[Produces("application/json")]
public class CategoriesController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public CategoriesController(ApplicationDbContext db) => _db = db;

    /// <summary>Toàn bộ danh mục (phẳng)</summary>
    [HttpGet]
    public async Task<ActionResult<List<CategoryDto>>> GetAll()
    {
        var cats = await _db.Categories
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
            .Select(c => new CategoryDto(
                c.Id, c.Name, c.Description, c.Slug, c.Icon, c.ParentCategoryId, null))
            .ToListAsync();
        return Ok(cats);
    }

    /// <summary>Chi tiết danh mục kèm danh mục con</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<CategoryDto>> GetById(int id)
    {
        var c = await _db.Categories
            .Include(c => c.SubCategories)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (c == null) return NotFound(new { message = "Danh mục không tồn tại." });

        return Ok(new CategoryDto(
            c.Id, c.Name, c.Description, c.Slug, c.Icon, c.ParentCategoryId,
            c.SubCategories.Select(s =>
                new CategoryDto(s.Id, s.Name, s.Description, s.Slug, s.Icon, s.ParentCategoryId, null))
            .ToList()));
    }

    /// <summary>Sản phẩm trong danh mục</summary>
    [HttpGet("{id:int}/products")]
    public async Task<ActionResult<PagedResult<ProductListDto>>> GetProducts(
        int id, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (!await _db.Categories.AnyAsync(c => c.Id == id))
            return NotFound(new { message = "Danh mục không tồn tại." });

        pageSize = Math.Clamp(pageSize, 1, 100);
        page     = Math.Max(1, page);

        var q = _db.Products
            .Include(p => p.Brand)
            .Include(p => p.Reviews)
            .Where(p => p.CategoryId == id && p.IsActive);

        var total = await q.CountAsync();
        var items = await q
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(p => new ProductListDto(
                p.Id, p.Name, p.Price, p.Stock, p.IsActive, p.ImageUrl,
                p.Category.Name, p.Brand.Name,
                p.Reviews.Any() ? p.Reviews.Average(r => (double)r.Rating) : (double?)null,
                p.Reviews.Count))
            .ToListAsync();

        return Ok(new PagedResult<ProductListDto>(
            items, page, pageSize, total,
            (int)Math.Ceiling(total / (double)pageSize)));
    }
}
