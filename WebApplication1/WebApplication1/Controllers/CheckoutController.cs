using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.Repositories;
using WebApplication1.Services;
using WebApplication1.ViewModels;
using System.Text.Json;

namespace WebApplication1.Controllers;

[Authorize(Roles = "Customer,Admin")]
public class CheckoutController : Controller
{
    private const decimal ShippingFee = 50000m;
    private readonly ICartService _cartService;
    private readonly IOrderRepository _orders;
    private readonly IDiscountRepository _discounts;
    private readonly ApplicationDbContext _context;
    private readonly IVnPayService _vnPay;
    private readonly IConfiguration _config;

    public CheckoutController(
        ICartService cartService,
        IOrderRepository orders,
        IDiscountRepository discounts,
        ApplicationDbContext context,
        IVnPayService vnPay,
        IConfiguration config)
    {
        _cartService = cartService;
        _orders = orders;
        _discounts = discounts;
        _context = context;
        _vnPay = vnPay;
        _config = config;
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
    public async Task<IActionResult> Shipping(CheckoutViewModel vm)
    {
        // Chỉ kiểm tra validation của các field Shipping.*
        var shippingKeys = ModelState.Keys
            .Where(k => !k.StartsWith("Shipping."))
            .ToList();
        foreach (var key in shippingKeys)
            ModelState.Remove(key);

        if (!ModelState.IsValid)
            return View("Index", new CheckoutViewModel
            {
                Step = 1,
                Shipping = vm.Shipping,
                Cart = await _cartService.GetCartAsync()
            });

        TempData["ShippingFullName"]    = vm.Shipping.FullName;
        TempData["ShippingPhone"]       = vm.Shipping.Phone;
        TempData["ShippingAddress"]     = vm.Shipping.ShippingAddress;
        TempData["ShippingNotes"]       = vm.Shipping.Notes;
        TempData.Keep();
        return RedirectToAction(nameof(Index), new { step = 2 });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PlaceOrder(PaymentViewModel model)
    {
        var cart = await _cartService.GetCartAsync();
        if (!cart.Items.Any())
            return RedirectToAction("Index", "Cart");

        var userId   = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var subtotal = cart.Subtotal;

        // Apply discount
        var discountCode   = TempData["DiscountCode"]?.ToString();
        var discountAmount = 0m;
        if (!string.IsNullOrEmpty(discountCode))
        {
            var discount = await _discounts.GetActiveByCodeAsync(discountCode, subtotal);
            if (discount != null)
            {
                discountAmount = discount.DiscountType == DiscountType.Percent
                    ? Math.Round(subtotal * discount.Value / 100, 0)
                    : discount.Value;
                discountAmount = Math.Min(discountAmount, subtotal);
                discount.UsedCount++;
                _discounts.Update(discount);
            }
            else
            {
                discountCode = null;
            }
        }

        var total = subtotal - discountAmount + ShippingFee;

        var order = new Order
        {
            UserId          = userId,
            FullName        = TempData["ShippingFullName"]?.ToString() ?? User.Identity?.Name ?? "",
            Phone           = TempData["ShippingPhone"]?.ToString() ?? "",
            ShippingAddress = TempData["ShippingAddress"]?.ToString() ?? "",
            Notes           = TempData["ShippingNotes"]?.ToString(),
            PaymentMethod   = model.PaymentMethod,
            PaymentStatus   = model.PaymentMethod == "VNPay" ? PaymentStatus.Pending : PaymentStatus.COD,
            DiscountCode    = discountCode,
            DiscountAmount  = discountAmount,
            Subtotal        = subtotal,
            ShippingFee     = ShippingFee,
            Total           = total,
            Status          = OrderStatus.Pending,
            Items = cart.Items.Select(i => new OrderItem
            {
                ProductId   = i.ProductId,
                ProductName = i.Name,
                UnitPrice   = i.UnitPrice,
                Quantity    = i.Quantity
            }).ToList()
        };

        // Kiểm tra & trừ tồn kho
        foreach (var line in cart.Items)
        {
            var product = await _context.Products.FindAsync(line.ProductId);
            if (product == null || product.Stock < line.Quantity)
            {
                TempData["Error"] = $"Sản phẩm \"{line.Name}\" không đủ hàng trong kho.";
                return RedirectToAction("Index", "Cart");
            }
            product.Stock -= line.Quantity;
        }

        await _orders.AddAsync(order);
        await _orders.SaveChangesAsync();

        // ── COD: xử lý xong, xóa giỏ hàng ngay ──
        if (model.PaymentMethod != "VNPay")
        {
            await _cartService.ClearCartAsync();
            return RedirectToAction(nameof(Confirmation), new { id = order.Id });
        }

        // ── VNPay: chuyển hướng sang cổng thanh toán ──
        var returnUrl  = _config["VnPay:ReturnUrl"] is { Length: > 0 } u
                       ? u
                       : Url.Action("VnPayReturn", "Checkout", null, Request.Scheme)!;
        var clientIp   = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        var paymentUrl = _vnPay.CreatePaymentUrl(order, returnUrl, clientIp);

        return Redirect(paymentUrl);
    }

    // ── Callback từ VNPay sau khi khách thanh toán ──
    [AllowAnonymous]
    public async Task<IActionResult> VnPayReturn()
    {
        var result = _vnPay.ProcessReturn(Request.Query);

        if (!result.IsValidSignature)
        {
            TempData["Error"] = "Chữ ký không hợp lệ. Vui lòng liên hệ hỗ trợ.";
            return RedirectToAction("Index", "Home");
        }

        var order = await _orders.GetWithItemsAsync(result.OrderId);
        if (order == null)
        {
            TempData["Error"] = "Không tìm thấy đơn hàng.";
            return RedirectToAction("Index", "Home");
        }

        if (result.IsSuccess)
        {
            order.PaymentStatus      = PaymentStatus.Paid;
            order.VnPayTransactionId = result.TransactionId;
            _context.Orders.Update(order);
            await _context.SaveChangesAsync();
            await _cartService.ClearCartAsync();

            TempData["Success"] = $"Thanh toán thành công! Mã giao dịch: {result.TransactionId}";
        }
        else
        {
            // Hoàn kho khi thanh toán thất bại
            order.PaymentStatus = PaymentStatus.Failed;
            foreach (var item in order.Items)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product != null) product.Stock += item.Quantity;
            }
            _context.Orders.Update(order);
            await _context.SaveChangesAsync();

            TempData["Error"] = $"Thanh toán thất bại: {result.Message} (Mã lỗi: {result.ResponseCode})";
        }

        return RedirectToAction(nameof(Confirmation), new { id = order.Id });
    }

    [HttpPost]
    public async Task<IActionResult> ApplyDiscount([FromBody] ApplyDiscountRequest req)
    {
        var cart = await _cartService.GetCartAsync();
        var discount = await _discounts.GetActiveByCodeAsync(req.Code ?? string.Empty, cart.Subtotal);

        if (discount == null)
            return Json(new { success = false, message = "Mã giảm giá không hợp lệ hoặc không áp dụng được." });

        var discountAmount = discount.DiscountType == DiscountType.Percent
            ? Math.Round(cart.Subtotal * discount.Value / 100, 0)
            : discount.Value;
        discountAmount = Math.Min(discountAmount, cart.Subtotal);

        TempData["DiscountCode"] = discount.Code;
        TempData.Keep("DiscountCode");

        return Json(new
        {
            success = true,
            code = discount.Code,
            discountAmount,
            message = $"Áp dụng thành công: {discount.Name}"
        });
    }

    public async Task<IActionResult> Confirmation(int id)
    {
        var order = await _orders.GetWithItemsAsync(id);
        if (order == null)
            return NotFound();

        // Cho phép xem nếu đã đăng nhập và sở hữu đơn, hoặc vừa được redirect từ VNPay
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId != null && order.UserId != userId)
            return NotFound();

        return View(new OrderConfirmationViewModel { Order = order });
    }
}
