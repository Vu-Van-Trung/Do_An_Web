using Microsoft.AspNetCore.Mvc;
using WebApplication1.Repositories;

namespace WebApplication1.Controllers;

public class CategoryController : Controller
{
    private readonly ICategoryRepository _categories;

    public CategoryController(ICategoryRepository categories)
    {
        _categories = categories;
    }

    public async Task<IActionResult> Index()
    {
        var groups = await _categories.GetRootCategoriesAsync();
        return View(groups);
    }
}
