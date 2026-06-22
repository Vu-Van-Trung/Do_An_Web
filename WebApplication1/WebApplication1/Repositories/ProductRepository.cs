using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.ViewModels;

namespace WebApplication1.Repositories;

public class ProductRepository : Repository<Product>, IProductRepository
{
    public ProductRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Product?> GetWithDetailsAsync(int id) =>
        await Context.Products
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .Include(p => p.Specifications)
            .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

    public async Task<Product?> GetBySlugAsync(string slug) =>
        await Context.Products
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .Include(p => p.Specifications)
            .FirstOrDefaultAsync(p => p.Slug == slug && p.IsActive);

    public async Task<PagedResult<Product>> GetFilteredAsync(ProductFilter filter)
    {
        var query = Context.Products
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .Include(p => p.Specifications)
            .Where(p => p.IsActive);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim().ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(term) ||
                (p.Description != null && p.Description.ToLower().Contains(term)) ||
                p.Brand.Name.ToLower().Contains(term) ||
                p.Category.Name.ToLower().Contains(term));
        }

        if (filter.CategoryId.HasValue)
            query = query.Where(p => p.CategoryId == filter.CategoryId);

        if (filter.BrandId.HasValue)
            query = query.Where(p => p.BrandId == filter.BrandId);

        if (filter.MinPrice.HasValue)
            query = query.Where(p => p.Price >= filter.MinPrice);

        if (filter.MaxPrice.HasValue)
            query = query.Where(p => p.Price <= filter.MaxPrice);

        if (!string.IsNullOrWhiteSpace(filter.Connection))
            query = query.Where(p => p.Specifications.Any(s => s.Key == "Connection" && s.Value == filter.Connection));

        if (!string.IsNullOrWhiteSpace(filter.SwitchType))
            query = query.Where(p => p.Specifications.Any(s => s.Key == "Switch Type" && s.Value == filter.SwitchType));

        if (!string.IsNullOrWhiteSpace(filter.Dpi))
            query = query.Where(p => p.Specifications.Any(s => s.Key == "DPI" && s.Value == filter.Dpi));

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return new PagedResult<Product>
        {
            Items = items,
            TotalCount = total,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<IEnumerable<string>> GetSpecValuesAsync(string key) =>
        await Context.ProductSpecifications
            .Where(s => s.Key == key)
            .Select(s => s.Value)
            .Distinct()
            .OrderBy(v => v)
            .ToListAsync();
}
