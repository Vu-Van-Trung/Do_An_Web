using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models;
using WebApplication1.Repositories;
using WebApplication1.ViewModels;

namespace WebApplication1.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,Manager")]
public class DiscountsController : Controller
{
    private readonly IDiscountRepository _discounts;

    public DiscountsController(IDiscountRepository discounts)
    {
        _discounts = discounts;
    }

    public async Task<IActionResult> Index()
    {
        var list = await _discounts.GetAllAsync();
        return View(list.OrderByDescending(d => d.Id));
    }

    public IActionResult Create() => View(new DiscountFormViewModel
    {
        StartDate = DateTime.Today,
        EndDate = DateTime.Today.AddMonths(1)
    });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DiscountFormViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var existing = await _discounts.GetByCodeAsync(vm.Code);
        if (existing != null)
        {
            ModelState.AddModelError("Code", "Mã giảm giá này đã tồn tại.");
            return View(vm);
        }

        await _discounts.AddAsync(new Discount
        {
            Code = vm.Code.ToUpper().Trim(),
            Name = vm.Name,
            DiscountType = vm.DiscountType,
            Value = vm.Value,
            MinOrderAmount = vm.MinOrderAmount,
            MaxUsage = vm.MaxUsage,
            StartDate = vm.StartDate.ToUniversalTime(),
            EndDate = vm.EndDate.ToUniversalTime(),
            IsActive = vm.IsActive
        });
        await _discounts.SaveChangesAsync();

        TempData["Success"] = $"Đã tạo mã giảm giá {vm.Code.ToUpper()}.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var d = await _discounts.GetByIdAsync(id);
        if (d == null) return NotFound();

        return View(new DiscountFormViewModel
        {
            Id = d.Id,
            Code = d.Code,
            Name = d.Name,
            DiscountType = d.DiscountType,
            Value = d.Value,
            MinOrderAmount = d.MinOrderAmount,
            MaxUsage = d.MaxUsage,
            StartDate = d.StartDate.ToLocalTime(),
            EndDate = d.EndDate.ToLocalTime(),
            IsActive = d.IsActive
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, DiscountFormViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var d = await _discounts.GetByIdAsync(id);
        if (d == null) return NotFound();

        var duplicate = await _discounts.GetByCodeAsync(vm.Code);
        if (duplicate != null && duplicate.Id != id)
        {
            ModelState.AddModelError("Code", "Mã giảm giá này đã tồn tại.");
            return View(vm);
        }

        d.Code = vm.Code.ToUpper().Trim();
        d.Name = vm.Name;
        d.DiscountType = vm.DiscountType;
        d.Value = vm.Value;
        d.MinOrderAmount = vm.MinOrderAmount;
        d.MaxUsage = vm.MaxUsage;
        d.StartDate = vm.StartDate.ToUniversalTime();
        d.EndDate = vm.EndDate.ToUniversalTime();
        d.IsActive = vm.IsActive;

        _discounts.Update(d);
        await _discounts.SaveChangesAsync();

        TempData["Success"] = $"Đã cập nhật mã {d.Code}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var d = await _discounts.GetByIdAsync(id);
        if (d == null) return NotFound();

        _discounts.Remove(d);
        await _discounts.SaveChangesAsync();

        TempData["Success"] = $"Đã xóa mã {d.Code}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id)
    {
        var d = await _discounts.GetByIdAsync(id);
        if (d == null) return NotFound();

        d.IsActive = !d.IsActive;
        _discounts.Update(d);
        await _discounts.SaveChangesAsync();

        TempData["Success"] = d.IsActive ? $"Đã kích hoạt mã {d.Code}." : $"Đã tắt mã {d.Code}.";
        return RedirectToAction(nameof(Index));
    }
}
