using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models;
using WebApplication1.Repositories;

namespace WebApplication1.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,Manager")]
public class BrandsController : Controller
{
    private readonly IBrandRepository _brands;
    private readonly IWebHostEnvironment _env; // Khai báo thêm biến môi trường để lưu tệp

    // Bổ sung IWebHostEnvironment vào Hàm khởi tạo (Constructor)
    public BrandsController(IBrandRepository brands, IWebHostEnvironment env)
    {
        _brands = brands;
        _env = env;
    }

    public async Task<IActionResult> Index() => View(await _brands.GetAllAsync());

    public IActionResult Create() => View(new Brand());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Brand model, IFormFile? logoFile)
    {
        if (!ModelState.IsValid) return View(model);

        // Xử lý upload ảnh khi tạo mới thương hiệu
        if (logoFile != null && logoFile.Length > 0)
        {
            model.LogoUrl = await SaveLogoAsync(logoFile);
        }
        else
        {
            // Gán ảnh mặc định nếu admin không upload tệp tin
            model.LogoUrl = "/images/brands/placeholder.svg";
        }

        await _brands.AddAsync(model);
        await _brands.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var item = await _brands.GetByIdAsync(id);
        if (item == null) return NotFound();
        return View(item);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Brand model, IFormFile? logoFile)
    {
        if (id != model.Id) return NotFound();
        if (!ModelState.IsValid) return View(model);

        // Lấy dữ liệu cũ từ DB ra để so sánh và xử lý ảnh cũ
        var existingBrand = await _brands.GetByIdAsync(id);
        if (existingBrand == null) return NotFound();

        // Cập nhật các thông tin cơ bản từ model sang đối tượng theo dõi của EF
        existingBrand.Name = model.Name;

        if (logoFile != null && logoFile.Length > 0)
        {
            // Tối ưu: Xóa logo cũ vật lý trên ổ đĩa nếu nó không phải ảnh mặc định
            if (!string.IsNullOrEmpty(existingBrand.LogoUrl) && !existingBrand.LogoUrl.Contains("placeholder.svg"))
            {
                var oldFilePath = Path.Combine(_env.WebRootPath, existingBrand.LogoUrl.TrimStart('/'));
                if (System.IO.File.Exists(oldFilePath))
                {
                    System.IO.File.Delete(oldFilePath);
                }
            }

            // Lưu logo mới và gán đường dẫn
            existingBrand.LogoUrl = await SaveLogoAsync(logoFile);
        }
        // Trường hợp không chọn file mới: Giữ nguyên đường dẫn cũ có sẵn trong DB (không chỉnh sửa trường LogoUrl)

        _brands.Update(existingBrand);
        await _brands.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _brands.GetByIdAsync(id);
        if (item == null) return NotFound();

        // Tối ưu: Khi xóa thương hiệu, tiến hành dọn dẹp file ảnh logo vật lý trong hệ thống
        if (!string.IsNullOrEmpty(item.LogoUrl) && !item.LogoUrl.Contains("placeholder.svg"))
        {
            var filePath = Path.Combine(_env.WebRootPath, item.LogoUrl.TrimStart('/'));
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
        }

        _brands.Remove(item);
        await _brands.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // Hàm phụ trợ xử lý lưu file vật lý xuống thư mục wwwroot/images/brands
    private async Task<string> SaveLogoAsync(IFormFile file)
    {
        var uploads = Path.Combine(_env.WebRootPath, "images", "brands");
        var isDev = false;

        #if DEBUG
        // Trong chế độ debug, lưu trực tiếp vào thư mục nguồn để tránh mất file khi rebuild
        var sourcePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "brands");
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
            var binUploads = Path.Combine(_env.WebRootPath, "images", "brands");
            var binPath = Path.Combine(binUploads, fileName);
            if (!string.Equals(path, binPath, StringComparison.OrdinalIgnoreCase))
            {
                Directory.CreateDirectory(binUploads);
                System.IO.File.Copy(path, binPath, true);
            }
        }
        #endif

        return $"/images/brands/{fileName}";
    }
}