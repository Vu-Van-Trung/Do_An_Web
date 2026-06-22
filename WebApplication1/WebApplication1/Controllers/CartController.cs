using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using WebApplication1.Data;
using WebApplication1.Services;

namespace WebApplication1.Controllers;

public class CartController : Controller
{
    private const string SelectedItemsKey = "SelectedCartItems";
    private readonly ICartService _cartService;
    private readonly ApplicationDbContext _context;

    public CartController(ICartService cartService, ApplicationDbContext context)
    {
        _cartService = cartService;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        return View(await _cartService.GetCartAsync());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CheckoutSelected([FromForm] int[] selectedItems)
    {
        if (selectedItems == null || selectedItems.Length == 0)
        {
            TempData["Error"] = "Vui lòng chọn ít nhất một sản phẩm để thanh toán.";
            return RedirectToAction(nameof(Index));
        }
        HttpContext.Session.SetString(SelectedItemsKey, JsonSerializer.Serialize(selectedItems));
        return RedirectToAction("Index", "Checkout");
    }

    [HttpPost]
    public async Task<IActionResult> Add(int productId, int quantity = 1)
    {
        try
        {
            await _cartService.AddToCartAsync(productId, quantity);
            var count = await _cartService.GetItemCountAsync();
            if (Request.Headers.Accept.ToString().Contains("application/json") ||
                Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, count });
            }
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return Json(new { success = false, message = ex.Message });
            TempData["Error"] = ex.Message;
            var product = await _context.Products.FindAsync(productId);
            return RedirectToAction("Details", "Product", new { slug = product?.Slug ?? productId.ToString() });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Update(int cartItemId, int quantity)
    {
        await _cartService.UpdateQuantityAsync(cartItemId, quantity);
        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            var cart = await _cartService.GetCartAsync();
            return Json(new
            {
                success = true,
                subtotal = cart.Subtotal,
                count = cart.ItemCount
            });
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Remove(int cartItemId)
    {
        await _cartService.RemoveItemAsync(cartItemId);
        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            var cart = await _cartService.GetCartAsync();
            return Json(new { success = true, subtotal = cart.Subtotal, count = cart.ItemCount });
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Count()
    {
        return Json(new { count = await _cartService.GetItemCountAsync() });
    }
}
