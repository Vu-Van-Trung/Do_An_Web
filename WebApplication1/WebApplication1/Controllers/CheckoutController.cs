using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.Repositories;
using WebApplication1.Services;
using WebApplication1.ViewModels;

namespace WebApplication1.Controllers;

[Authorize(Roles = "Customer,Admin")]
public class CheckoutController : Controller
{
    private const decimal ShippingFee = 50000m;
    private readonly ICartService _cartService;
    private readonly IOrderRepository _orders;
    private readonly ApplicationDbContext _context;

    public CheckoutController(ICartService cartService, IOrderRepository orders, ApplicationDbContext context)
    {
        _cartService = cartService;
        _orders = orders;
        _context = context;
    }

    public async Task<IActionResult> Index(int step = 1)
    {
        var cart = await _cartService.GetCartAsync();
        if (!cart.Items.Any())
            return RedirectToAction("Index", "Cart");

        return View(new CheckoutViewModel { Step = step, Cart = cart });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Shipping(ShippingViewModel model)
    {
        if (!ModelState.IsValid)
            return View("Index", new CheckoutViewModel
            {
                Step = 1,
                Shipping = model,
                Cart = await _cartService.GetCartAsync()
            });

        TempData["ShippingFullName"] = model.FullName;
        TempData["ShippingPhone"] = model.Phone;
        TempData["ShippingAddress"] = model.ShippingAddress;
        TempData["ShippingNotes"] = model.Notes;
        return RedirectToAction(nameof(Index), new { step = 2 });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PlaceOrder(PaymentViewModel model)
    {
        var cart = await _cartService.GetCartAsync();
        if (!cart.Items.Any())
            return RedirectToAction("Index", "Cart");

        if (model.PaymentMethod == "Card" &&
            (string.IsNullOrWhiteSpace(model.CardNumber) || model.CardNumber.Length < 12))
        {
            ModelState.AddModelError(nameof(model.CardNumber), "Enter a valid mock card number.");
            return View("Index", new CheckoutViewModel
            {
                Step = 2,
                Payment = model,
                Cart = cart
            });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var subtotal = cart.Subtotal;
        var order = new Order
        {
            UserId = userId,
            FullName = TempData["ShippingFullName"]?.ToString() ?? User.Identity?.Name ?? "",
            Phone = TempData["ShippingPhone"]?.ToString() ?? "",
            ShippingAddress = TempData["ShippingAddress"]?.ToString() ?? "",
            Notes = TempData["ShippingNotes"]?.ToString(),
            PaymentMethod = model.PaymentMethod,
            Subtotal = subtotal,
            ShippingFee = ShippingFee,
            Total = subtotal + ShippingFee,
            Status = OrderStatus.Pending,
            Items = cart.Items.Select(i => new OrderItem
            {
                ProductId = i.ProductId,
                ProductName = i.Name,
                UnitPrice = i.UnitPrice,
                Quantity = i.Quantity
            }).ToList()
        };

        foreach (var line in cart.Items)
        {
            var product = await _context.Products.FindAsync(line.ProductId);
            if (product == null || product.Stock < line.Quantity)
            {
                TempData["Error"] = $"Insufficient stock for {line.Name}.";
                return RedirectToAction("Index", "Cart");
            }
            product.Stock -= line.Quantity;
        }

        await _orders.AddAsync(order);
        await _orders.SaveChangesAsync();
        await _cartService.ClearCartAsync();

        return RedirectToAction(nameof(Confirmation), new { id = order.Id });
    }

    public async Task<IActionResult> Confirmation(int id)
    {
        var order = await _orders.GetWithItemsAsync(id);
        if (order == null || order.UserId != User.FindFirstValue(ClaimTypes.NameIdentifier))
            return NotFound();
        return View(new OrderConfirmationViewModel { Order = order });
    }
}
