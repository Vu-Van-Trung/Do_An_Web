using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models;
using WebApplication1.Repositories;

namespace WebApplication1.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "QuanLyDanhMuc")]
public class CategoriesController : Controller
{
    private readonly ICategoryRepository _categories;

    public CategoriesController(ICategoryRepository categories)
    {
        _categories = categories;
    }

    public async Task<IActionResult> Index() => View(await _categories.GetAllAsync());

    public IActionResult Create() => View(new Category());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Category model)
    {
        if (!ModelState.IsValid) return View(model);
        await _categories.AddAsync(model);
        await _categories.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var item = await _categories.GetByIdAsync(id);
        if (item == null) return NotFound();
        return View(item);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Category model)
    {
        if (id != model.Id) return NotFound();
        if (!ModelState.IsValid) return View(model);
        _categories.Update(model);
        await _categories.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _categories.GetByIdAsync(id);
        if (item == null) return NotFound();
        _categories.Remove(item);
        await _categories.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
