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
[Authorize(Roles = "Admin,Manager")]
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

        var product = await MapProductAsync(new Product(), model);
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
            await PopulateLookupsAsync(model);
            return View(model);
        }

        // Lấy sản phẩm kèm theo ảnh cũ từ DB trước khi cập nhật
        var product = await _context.Products.Include(p => p.Specifications).FirstOrDefaultAsync(p => p.Id == id);
        if (product == null) return NotFound();

        // Tối ưu hóa: Nếu admin chọn upload file ảnh mới VÀ sản phẩm hiện tại đang có ảnh thực tế (không phải placeholder)
        // tiến hành xóa file ảnh cũ trên ổ đĩa Server để tiết kiệm dung lượng bộ nhớ.
        if (model.ImageFile != null && model.ImageFile.Length > 0 && !string.IsNullOrEmpty(product.ImageUrl))
        {
            if (!product.ImageUrl.Contains("placeholder.svg"))
            {
                var oldFilePath = Path.Combine(_env.WebRootPath, product.ImageUrl.TrimStart('/'));
                if (System.IO.File.Exists(oldFilePath))
                {
                    System.IO.File.Delete(oldFilePath);
                }
            }
        }

        // Tối ưu hóa: Nếu admin chọn upload các file ảnh phụ mới VÀ sản phẩm hiện tại đang có các ảnh phụ cũ
        // tiến hành xóa các file ảnh phụ cũ trên ổ đĩa Server.
        if (model.SecondaryImageFiles != null && model.SecondaryImageFiles.Count > 0 && model.SecondaryImageFiles.Any(f => f.Length > 0) && !string.IsNullOrEmpty(product.SecondaryImageUrls))
        {
            var oldUrls = product.SecondaryImageUrls.Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (var oldUrl in oldUrls)
            {
                if (!oldUrl.Contains("placeholder.svg") && !oldUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    var oldFilePath = Path.Combine(_env.WebRootPath, oldUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Delete(oldFilePath);
                    }
                }
            }
        }

        _context.ProductSpecifications.RemoveRange(product.Specifications);
        await MapProductAsync(product, model);
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
        if (!string.IsNullOrEmpty(product.ImageUrl) && !product.ImageUrl.Contains("placeholder.svg"))
        {
            var filePath = Path.Combine(_env.WebRootPath, product.ImageUrl.TrimStart('/'));
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
        }

        // Tối ưu hóa: Xóa các tệp ảnh phụ vật lý trong thư mục uploads khi sản phẩm bị xóa
        if (!string.IsNullOrEmpty(product.SecondaryImageUrls))
        {
            var oldUrls = product.SecondaryImageUrls.Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (var oldUrl in oldUrls)
            {
                if (!oldUrl.Contains("placeholder.svg") && !oldUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    var oldFilePath = Path.Combine(_env.WebRootPath, oldUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Delete(oldFilePath);
                    }
                }
            }
        }

        _products.Remove(product);
        await _products.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private async Task<Product> MapProductAsync(Product product, ProductFormViewModel model)
    {
        product.Name = model.Name;
        product.Slug = Product.GenerateSlug(model.Name);
        product.Description = model.Description;
        product.Price = model.Price;
        product.Stock = model.Stock;
        product.CategoryId = model.CategoryId;
        product.BrandId = model.BrandId;
        product.IsActive = model.IsActive;

        // Cập nhật đường dẫn ảnh: Ưu tiên ảnh mới upload -> Tiếp đến là đường dẫn ảnh cũ từ Model -> Cuối cùng mới dùng placeholder
        var newImagePath = await SaveImageAsync(model);
        if (newImagePath != null)
        {
            product.ImageUrl = newImagePath;
        }
        else if (!string.IsNullOrWhiteSpace(model.ImageUrl))
        {
            product.ImageUrl = model.ImageUrl;
        }
        else
        {
            product.ImageUrl = "/images/products/placeholder.svg";
        }

        // Cập nhật đường dẫn ảnh phụ: Ưu tiên các ảnh phụ mới upload -> Tiếp đến là đường dẫn ảnh phụ cũ từ Model
        var newSecondaryPaths = await SaveSecondaryImagesAsync(model);
        if (newSecondaryPaths.Count > 0)
        {
            product.SecondaryImageUrls = string.Join(",", newSecondaryPaths);
        }
        else if (!string.IsNullOrWhiteSpace(model.SecondaryImageUrls))
        {
            product.SecondaryImageUrls = model.SecondaryImageUrls;
        }

        product.Specifications = model.Specifications
            .Where(s => !string.IsNullOrWhiteSpace(s.Key) && !string.IsNullOrWhiteSpace(s.Value))
            .Select(s => new ProductSpecification { Key = s.Key.Trim(), Value = s.Value.Trim() })
            .ToList();
        return product;
    }

    private async Task<string?> SaveImageAsync(ProductFormViewModel model)
    {
        if (model.ImageFile == null || model.ImageFile.Length == 0)
            return null;

        var uploads = Path.Combine(_env.WebRootPath, "images", "uploads");
        Directory.CreateDirectory(uploads);
        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(model.ImageFile.FileName)}";
        var path = Path.Combine(uploads, fileName);
        await using var stream = System.IO.File.Create(path);
        await model.ImageFile.CopyToAsync(stream);
        return $"/images/uploads/{fileName}";
    }

    private async Task<List<string>> SaveSecondaryImagesAsync(ProductFormViewModel model)
    {
        if (model.SecondaryImageFiles == null || model.SecondaryImageFiles.Count == 0)
            return new List<string>();

        var list = new List<string>();
        var uploads = Path.Combine(_env.WebRootPath, "images", "uploads");
        Directory.CreateDirectory(uploads);
        foreach (var file in model.SecondaryImageFiles)
        {
            if (file.Length == 0) continue;
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var path = Path.Combine(uploads, fileName);
            await using var stream = System.IO.File.Create(path);
            await file.CopyToAsync(stream);
            list.Add($"/images/uploads/{fileName}");
        }
        return list;
    }

    private async Task PopulateLookupsAsync(ProductFormViewModel? model = null)
    {
        ViewBag.Categories = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name", model?.CategoryId);
        ViewBag.Brands = new SelectList(await _context.Brands.ToListAsync(), "Id", "Name", model?.BrandId);
    }
}