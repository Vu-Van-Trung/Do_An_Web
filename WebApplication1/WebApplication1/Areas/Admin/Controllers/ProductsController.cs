using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.Repositories;
using WebApplication1.ViewModels;

namespace WebApplication1.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "QuanLySanPham")]
public class ProductsController : Controller
{
    private readonly IProductRepository _products;
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _env;

    public ProductsController(IProductRepository products, ApplicationDbContext context, IWebHostEnvironment env)
    {
        _products = products;
        _context = context;
        _env = env;
    }

    public async Task<IActionResult> Index() =>
        View(await _context.Products.Include(p => p.Brand).Include(p => p.Category).OrderByDescending(p => p.Id).ToListAsync());

    public async Task<IActionResult> Create()
    {
        await PopulateLookupsAsync();
        return View(new ProductFormViewModel { Specifications = new List<SpecInput> { new(), new() } });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateLookupsAsync(model);
            return View(model);
        }

        var product = await MapProductAsync(new Product(), model, isEdit: false);
        await _products.AddAsync(product);
        await _products.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var product = await _context.Products.Include(p => p.Specifications).FirstOrDefaultAsync(p => p.Id == id);
        if (product == null) return NotFound();

        var model = new ProductFormViewModel
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            Stock = product.Stock,
            CategoryId = product.CategoryId,
            BrandId = product.BrandId,
            ImageUrl = product.ImageUrl, // Giữ lại ImageUrl cũ để truyền sang Form hiển thị và nhận lại khi Post
            SecondaryImageUrls = product.SecondaryImageUrls,
            SecondaryImages = ParseSecondaryUrls(product.SecondaryImageUrls)
                .Select(url => new SecondaryImageEditItem { Url = url })
                .ToList(),
            IsActive = product.IsActive,
            Specifications = product.Specifications.Select(s => new SpecInput { Key = s.Key, Value = s.Value }).ToList()
        };
        await PopulateLookupsAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ProductFormViewModel model)
    {
        if (id != model.Id) return NotFound();
        if (!ModelState.IsValid)
        {
            if (model.SecondaryImages.Count == 0)
            {
                var existingUrls = await _context.Products.AsNoTracking()
                    .Where(p => p.Id == id)
                    .Select(p => p.SecondaryImageUrls)
                    .FirstOrDefaultAsync();
                model.SecondaryImages = ParseSecondaryUrls(existingUrls)
                    .Select(url => new SecondaryImageEditItem { Url = url })
                    .ToList();
            }
            await PopulateLookupsAsync(model);
            return View(model);
        }

        // Lấy sản phẩm kèm theo ảnh cũ từ DB trước khi cập nhật
        var product = await _context.Products.Include(p => p.Specifications).FirstOrDefaultAsync(p => p.Id == id);
        if (product == null) return NotFound();

        // Tối ưu hóa: Nếu admin chọn upload file ảnh mới VÀ sản phẩm hiện tại đang có ảnh thực tế (không phải placeholder)
        // tiến hành xóa file ảnh cũ trên ổ đĩa Server để tiết kiệm dung lượng bộ nhớ.
        if (model.ImageFile != null && model.ImageFile.Length > 0 && !string.IsNullOrEmpty(product.ImageUrl))
            DeleteImageFile(product.ImageUrl, null, ParseSecondaryUrls(product.SecondaryImageUrls));

        _context.ProductSpecifications.RemoveRange(product.Specifications);
        await MapProductAsync(product, model, isEdit: true);
        _products.Update(product);
        await _products.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _products.GetByIdAsync(id);
        if (product == null) return NotFound();

        // Tối ưu hóa: Xóa tệp ảnh vật lý trong thư mục uploads khi sản phẩm bị xóa hoàn toàn khỏi hệ thống
        DeleteImageFile(product.ImageUrl ?? "", null);
        foreach (var url in ParseSecondaryUrls(product.SecondaryImageUrls))
            DeleteImageFile(url, product.ImageUrl);

        _products.Remove(product);
        await _products.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private async Task<Product> MapProductAsync(Product product, ProductFormViewModel model, bool isEdit)
    {
        product.Name = model.Name;
        product.Slug = Product.GenerateSlug(model.Name);
        product.Description = model.Description;
        product.Price = model.Price;
        product.Stock = model.Stock;
        product.CategoryId = model.CategoryId;
        product.BrandId = model.BrandId;
        product.IsActive = model.IsActive;

        var newImagePath = await SaveImageAsync(model.ImageFile);
        if (newImagePath != null)
        {
            product.ImageUrl = newImagePath;
        }
        else if (!string.IsNullOrWhiteSpace(model.ImageUrl))
        {
            product.ImageUrl = model.ImageUrl;
        }
        else if (!isEdit)
        {
            product.ImageUrl = "/images/products/placeholder.svg";
        }

        await ApplySecondaryImagesAsync(product, model, isEdit);

        product.Specifications = model.Specifications
            .Where(s => !string.IsNullOrWhiteSpace(s.Key) && !string.IsNullOrWhiteSpace(s.Value))
            .Select(s => new ProductSpecification { Key = s.Key.Trim(), Value = s.Value.Trim() })
            .ToList();
        return product;
    }

    private async Task ApplySecondaryImagesAsync(Product product, ProductFormViewModel model, bool isEdit)
    {
        if (!isEdit)
        {
            var newPaths = await SaveSecondaryImagesAsync(model.SecondaryImageFiles);
            product.SecondaryImageUrls = newPaths.Count > 0 ? string.Join(",", newPaths) : null;
            return;
        }

        var currentUrls = ParseSecondaryUrls(product.SecondaryImageUrls);
        var result = new List<string>();

        foreach (var item in model.SecondaryImages)
        {
            var url = item.Url.Trim();
            if (string.IsNullOrEmpty(url) || !currentUrls.Contains(url))
                continue;

            if (item.ReplacementFile != null && item.ReplacementFile.Length > 0)
            {
                DeleteImageFile(url, product.ImageUrl, result);
                var newUrl = await SaveImageAsync(item.ReplacementFile);
                if (newUrl != null)
                    result.Add(newUrl);
            }
            else if (item.Remove)
            {
                DeleteImageFile(url, product.ImageUrl, result);
            }
            else
            {
                result.Add(url);
            }
        }

        var additional = await SaveSecondaryImagesAsync(model.SecondaryImageFiles);
        result.AddRange(additional);

        product.SecondaryImageUrls = result.Count > 0
            ? string.Join(",", result.Distinct(StringComparer.OrdinalIgnoreCase))
            : null;
    }

    private static List<string> ParseSecondaryUrls(string? urls) =>
        (urls ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(u => u.Trim())
            .Where(u => !string.IsNullOrEmpty(u))
            .ToList();

    private void DeleteImageFile(string url, string? primaryUrl, IEnumerable<string>? keepUrls = null)
    {
        if (string.IsNullOrWhiteSpace(url)) return;
        if (url.Contains("placeholder.svg", StringComparison.OrdinalIgnoreCase)) return;
        if (url.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return;
        if (string.Equals(url, primaryUrl, StringComparison.OrdinalIgnoreCase)) return;
        if (keepUrls != null && keepUrls.Any(u => string.Equals(u, url, StringComparison.OrdinalIgnoreCase))) return;

        var filePath = Path.Combine(_env.WebRootPath, url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (System.IO.File.Exists(filePath))
            System.IO.File.Delete(filePath);
    }

    private async Task<string?> SaveImageAsync(IFormFile? file)
    {
        if (file == null || file.Length == 0)
            return null;

        var uploads = Path.Combine(_env.WebRootPath, "images", "uploads");
        var isDev = false;

        #if DEBUG
        // Trong chế độ debug, nếu thư mục nguồn tồn tại, ưu tiên lưu vào thư mục nguồn để tránh mất file khi rebuild
        var sourcePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "uploads");
        if (Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot")))
        {
            uploads = sourcePath;
            isDev = true;
        }
        #endif

        Directory.CreateDirectory(uploads);
        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var path = Path.Combine(uploads, fileName);
        
        await using (var stream = System.IO.File.Create(path))
        {
            await file.CopyToAsync(stream);
        }

        #if DEBUG
        // Copy thêm sang thư mục chạy bin để ảnh hiển thị được ngay lập tức
        if (isDev)
        {
            var binUploads = Path.Combine(_env.WebRootPath, "images", "uploads");
            var binPath = Path.Combine(binUploads, fileName);
            if (!string.Equals(path, binPath, StringComparison.OrdinalIgnoreCase))
            {
                Directory.CreateDirectory(binUploads);
                System.IO.File.Copy(path, binPath, true);
            }
        }
        #endif

        return $"/images/uploads/{fileName}";
    }

    private async Task<List<string>> SaveSecondaryImagesAsync(List<IFormFile>? files)
    {
        if (files == null || files.Count == 0)
            return new List<string>();

        var list = new List<string>();
        foreach (var file in files)
        {
            var saved = await SaveImageAsync(file);
            if (saved != null)
                list.Add(saved);
        }
        return list;
    }

    private async Task PopulateLookupsAsync(ProductFormViewModel? model = null)
    {
        ViewBag.Categories = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name", model?.CategoryId);
        ViewBag.Brands = new SelectList(await _context.Brands.ToListAsync(), "Id", "Name", model?.BrandId);
    }
}