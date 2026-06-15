using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Controllers;

public class ComparisonController : Controller
{
    private readonly ApplicationDbContext _context;
    private const string SessionKey = "CompareProducts";

    public ComparisonController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var productIds = GetComparedProductIds();
        if (!productIds.Any())
        {
            return View(new List<Product>());
        }

        var products = await _context.Products
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .Include(p => p.Specifications)
            .Where(p => productIds.Contains(p.Id))
            .ToListAsync();

        // Order the list to match the order in session
        var orderedProducts = productIds
            .Select(id => products.FirstOrDefault(p => p.Id == id))
            .Where(p => p != null)
            .Cast<Product>()
            .ToList();

        return View(orderedProducts);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int productId)
    {
        var productExists = await _context.Products.AnyAsync(p => p.Id == productId);
        if (!productExists) return NotFound();

        var productIds = GetComparedProductIds();
        if (productIds.Contains(productId))
        {
            TempData["Success"] = "Sản phẩm đã có trong danh sách so sánh.";
        }
        else if (productIds.Count >= 4)
        {
            TempData["Error"] = "Chỉ có thể so sánh tối đa 4 sản phẩm cùng lúc!";
        }
        else
        {
            productIds.Add(productId);
            SaveComparedProductIds(productIds);
            TempData["Success"] = "Đã thêm sản phẩm vào danh sách so sánh.";
        }

        var referer = Request.Headers["Referer"].ToString();
        return string.IsNullOrEmpty(referer) ? RedirectToAction("Index") : Redirect(referer);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Remove(int productId)
    {
        var productIds = GetComparedProductIds();
        if (productIds.Contains(productId))
        {
            productIds.Remove(productId);
            SaveComparedProductIds(productIds);
            TempData["Success"] = "Đã xóa sản phẩm khỏi danh sách so sánh.";
        }

        var referer = Request.Headers["Referer"].ToString();
        return string.IsNullOrEmpty(referer) ? RedirectToAction("Index") : Redirect(referer);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Clear()
    {
        HttpContext.Session.Remove(SessionKey);
        TempData["Success"] = "Đã xóa toàn bộ danh sách so sánh.";

        var referer = Request.Headers["Referer"].ToString();
        return string.IsNullOrEmpty(referer) ? RedirectToAction("Index") : Redirect(referer);
    }

    private List<int> GetComparedProductIds()
    {
        var sessionStr = HttpContext.Session.GetString(SessionKey);
        if (string.IsNullOrEmpty(sessionStr))
        {
            return new List<int>();
        }

        return sessionStr.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(int.Parse)
            .ToList();
    }

    private void SaveComparedProductIds(List<int> ids)
    {
        var sessionStr = string.Join(",", ids);
        HttpContext.Session.SetString(SessionKey, sessionStr);
    }
}
