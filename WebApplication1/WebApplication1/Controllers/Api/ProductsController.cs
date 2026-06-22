using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Dtos;
using WebApplication1.Models;

namespace WebApplication1.Controllers.Api;

[ApiController]
[Route("api/products")]
[Produces("application/json")]
public class ProductsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public ProductsController(ApplicationDbContext db) => _db = db;

    /// <summary>Danh sách sản phẩm — hỗ trợ tìm kiếm, lọc và phân trang</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<ProductListDto>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] int? categoryId,
        [FromQuery] int? brandId,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        page     = Math.Max(1, page);

        var q = _db.Products
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Reviews)
            .Where(p => p.IsActive)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(p => p.Name.Contains(search) ||
                             (p.Description != null && p.Description.Contains(search)));
        if (categoryId.HasValue)
        {
            var catId = categoryId.Value;
            var subCategoryIds = await _db.Categories
                .Where(c => c.ParentCategoryId == catId)
                .Select(c => c.Id)
                .ToListAsync();

            var categoryIds = new List<int> { catId };
            categoryIds.AddRange(subCategoryIds);

            q = q.Where(p => categoryIds.Contains(p.CategoryId));
        }
        if (brandId.HasValue)    q = q.Where(p => p.BrandId    == brandId.Value);
        if (minPrice.HasValue)   q = q.Where(p => p.Price >= minPrice.Value);
        if (maxPrice.HasValue)   q = q.Where(p => p.Price <= maxPrice.Value);

        var total = await q.CountAsync();
        var items = await q
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
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

    /// <summary>Chi tiết một sản phẩm theo ID</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductDetailDto>> GetById(int id)
    {
        var p = await _db.Products
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Specifications)
            .Include(p => p.Reviews)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (p == null) return NotFound(new { message = "Sản phẩm không tồn tại." });

        var secondary = string.IsNullOrEmpty(p.SecondaryImageUrls)
            ? new List<string>()
            : p.SecondaryImageUrls.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

        return Ok(new ProductDetailDto(
            p.Id, p.Name, p.Description, p.Price, p.Stock, p.IsActive,
            p.ImageUrl, secondary, p.Category.Name, p.CategoryId,
            p.Brand.Name, p.BrandId, p.ShippingClass.ToString(),
            p.Reviews.Any() ? p.Reviews.Average(r => (double)r.Rating) : (double?)null,
            p.Reviews.Count,
            p.Specifications.Select(s => new SpecDto(s.Key, s.Value)).ToList(),
            p.CreatedAt));
    }

    /// <summary>Danh sách đánh giá của sản phẩm</summary>
    [HttpGet("{id:int}/reviews")]
    public async Task<ActionResult<List<ReviewDto>>> GetReviews(
        int id, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (!await _db.Products.AnyAsync(p => p.Id == id))
            return NotFound(new { message = "Sản phẩm không tồn tại." });

        pageSize = Math.Clamp(pageSize, 1, 50);
        page     = Math.Max(1, page);

        var reviews = await (
            from r in _db.ProductReviews
            where r.ProductId == id
            join u in _db.Users on r.UserId equals u.Id into ug
            from u in ug.DefaultIfEmpty()
            orderby r.CreatedAt descending
            select new ReviewDto(
                r.Id,
                u != null ? u.FullName : "Khách hàng",
                r.Rating, r.Comment, r.CreatedAt)
        ).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return Ok(reviews);
    }

    /// <summary>Thêm đánh giá sản phẩm (yêu cầu đăng nhập)</summary>
    [HttpPost("{id:int}/reviews")]
    [Authorize]
    public async Task<IActionResult> AddReview(int id, AddReviewRequest req)
    {
        if (!await _db.Products.AnyAsync(p => p.Id == id))
            return NotFound(new { message = "Sản phẩm không tồn tại." });

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (await _db.ProductReviews.AnyAsync(r => r.ProductId == id && r.UserId == userId))
            return Conflict(new { message = "Bạn đã đánh giá sản phẩm này rồi." });

        _db.ProductReviews.Add(new ProductReview
        {
            ProductId = id,
            UserId    = userId,
            Rating    = req.Rating,
            Comment   = req.Comment ?? string.Empty,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        return Created($"/api/products/{id}/reviews", new { message = "Đánh giá đã được ghi nhận." });
    }
}
