using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models;
using WebApplication1.Repositories;

namespace WebApplication1.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class BrandsController : Controller
{
    private readonly IBrandRepository _brands;

    public BrandsController(IBrandRepository brands)
    {
        _brands = brands;
    }

    public async Task<IActionResult> Index() => View(await _brands.GetAllAsync());

    public IActionResult Create() => View(new Brand());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Brand model)
    {
        if (!ModelState.IsValid) return View(model);
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
    public async Task<IActionResult> Edit(int id, Brand model)
    {
        if (id != model.Id) return NotFound();
        if (!ModelState.IsValid) return View(model);
        _brands.Update(model);
        await _brands.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _brands.GetByIdAsync(id);
        if (item == null) return NotFound();
        _brands.Remove(item);
        await _brands.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
