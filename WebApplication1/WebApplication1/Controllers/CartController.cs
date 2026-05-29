using Microsoft.AspNetCore.Mvc;
using WebApplication1.Services;

namespace WebApplication1.Controllers;

public class CartController : Controller
{
    private readonly ICartService _cartService;

    public CartController(ICartService cartService)
    {
        _cartService = cartService;
    }

    public async Task<IActionResult> Index()
    {
        return View(await _cartService.GetCartAsync());
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
            return RedirectToAction("Details", "Product", new { id = productId });
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
