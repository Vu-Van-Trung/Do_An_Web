using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Repositories;
using WebApplication1.ViewModels;

namespace WebApplication1.Controllers;

public class ProductController : Controller
{
    private readonly IProductRepository _products;
    private readonly ApplicationDbContext _context;

    public ProductController(IProductRepository products, ApplicationDbContext context)
    {
        _products = products;
        _context = context;
    }

    public async Task<IActionResult> Index(ProductFilter filter)
    {
        var vm = new CatalogViewModel
        {
            Filter = filter,
            Products = await _products.GetFilteredAsync(filter),
            Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync(),
            Brands = await _context.Brands.OrderBy(b => b.Name).ToListAsync(),
            Connections = await _products.GetSpecValuesAsync("Connection"),
            SwitchTypes = await _products.GetSpecValuesAsync("Switch Type"),
            DpiOptions = await _products.GetSpecValuesAsync("DPI")
        };
        return View(vm);
    }

    public async Task<IActionResult> Details(int id)
    {
        var product = await _products.GetWithDetailsAsync(id);
        if (product == null)
            return NotFound();
        return View(product);
    }
}
