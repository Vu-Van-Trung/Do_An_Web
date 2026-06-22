using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.Repositories;
using WebApplication1.Services;
using WebApplication1.ViewModels;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR; // BỔ SUNG: Thư viện SignalR

namespace WebApplication1.Controllers;

[Authorize(Policy = "DatHang")]
public class CheckoutController : Controller
{
    private const decimal ShippingFee = 50000m;
    private readonly ICartService _cartService;
    private readonly IOrderRepository _orders;
    private readonly IDiscountRepository _discounts;
    private readonly ApplicationDbContext _context;
    private readonly IVnPayService _vnPay;
    private readonly IConfiguration _config;
    private readonly IEmailService _emailService; // BỔ SUNG: Dịch vụ gửi email
    private readonly IHubContext<OrderHub> _hubContext; // BỔ SUNG: Trạm phát tín hiệu Real-time

    // CẬP NHẬT: Inject thêm IEmailService và IHubContext vào Constructor
    public CheckoutController(
        ICartService cartService,
        IOrderRepository orders,
        IDiscountRepository discounts,
        ApplicationDbContext context,
        IVnPayService vnPay,
        IConfiguration config,
        IEmailService emailService,
        IHubContext<OrderHub> hubContext)
    {
        _cartService = cartService;
        _orders = orders;
        _discounts = discounts;
        _context = context;
        _vnPay = vnPay;
        _config = config;
        _emailService = emailService;
        _hubContext = hubContext;
    }

    private decimal CalculateShippingFee(string address)
    {
        if (string.IsNullOrWhiteSpace(address)) return 30000m;
        bool isHCM = address.Contains("Hồ Chí Minh", StringComparison.OrdinalIgnoreCase) || 
                     address.Contains("HCM", StringComparison.OrdinalIgnoreCase);
        
        var random = new Random();
        if (isHCM)
        {
            return random.Next(20, 31) * 1000m;
        }
        else
        {
            return random.Next(50, 81) * 1000m;
        }
    }

    public async Task<IActionResult> Index(int step = 1)
    {
        var cart = await _cartService.GetCartAsync();
        if (!cart.Items.Any())
            return RedirectToAction("Index", "Cart");

        var vm = new CheckoutViewModel { Step = step, Cart = cart };

        var now = DateTime.UtcNow;
        List<Discount> userVouchers = new();
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId != null)
        {
            var orderCount = await _context.Orders.CountAsync(o => o.UserId == userId);
            var query = _context.Discounts
                .Where(d => d.IsActive && d.StartDate <= now && d.EndDate >= now
                            && (d.MaxUsage == null || d.UsedCount < d.MaxUsage));

            if (orderCount > 0)
            {
                query = query.Where(d => d.PromotionType != PromotionType.FirstOrder);
            }
            userVouchers = await query.ToListAsync();
        }
        ViewBag.UserVouchers = userVouchers;

        if (step == 1)
        {
            if (userId != null)
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user != null)
                {
                    vm.Shipping.FullName = user.FullName;
                    vm.Shipping.Phone = user.PhoneNumber ?? string.Empty;
                    vm.Shipping.ShippingAddress = user.Address ?? string.Empty;
                }
            }

            // Calculate shipping fee for Step 1 if default address is available
            if (!string.IsNullOrEmpty(vm.Shipping.ShippingAddress))
            {
                if (TempData["ShippingFee"] is string feeStr && decimal.TryParse(feeStr, out var fee))
                {
                    vm.ShippingFee = fee;
                }
                else
                {
                    vm.ShippingFee = CalculateShippingFee(vm.Shipping.ShippingAddress);
                    TempData["ShippingFee"] = vm.ShippingFee.ToString();
                }
            }
            else
            {
                vm.ShippingFee = 50000m;
            }

            var originalShippingFee1 = !string.IsNullOrEmpty(vm.Shipping.ShippingAddress)
                ? (TempData["ShippingFee"] is string originalFeeStr && decimal.TryParse(originalFeeStr, out var originalFee) ? originalFee : CalculateShippingFee(vm.Shipping.ShippingAddress))
                : 50000m;
            ViewBag.OriginalShippingFee = originalShippingFee1;

            // Apply free shipping discount in Step 1 if voucher is applied
            var appliedCode1 = TempData["DiscountCode"]?.ToString() ?? HttpContext.Session.GetString("AppliedCouponCode");
            if (!string.IsNullOrEmpty(appliedCode1))
            {
                var discount = await _discounts.GetActiveByCodeAsync(appliedCode1, cart.Subtotal);
                if (discount != null && discount.PromotionType == PromotionType.FreeShipping)
                {
                    vm.ShippingFee = 0m;
                }
            }

            TempData.Keep();
        }
        else
        {
            vm.Shipping.FullName = TempData["ShippingFullName"]?.ToString() ?? string.Empty;
            vm.Shipping.Phone = TempData["ShippingPhone"]?.ToString() ?? string.Empty;
            vm.Shipping.ShippingAddress = TempData["ShippingAddress"]?.ToString() ?? string.Empty;
            vm.Shipping.Notes = TempData["ShippingNotes"]?.ToString();
            
            if (TempData["ShippingFee"] is string feeStr && decimal.TryParse(feeStr, out var fee))
            {
                vm.ShippingFee = fee;
            }
            else
            {
                vm.ShippingFee = CalculateShippingFee(vm.Shipping.ShippingAddress);
                TempData["ShippingFee"] = vm.ShippingFee.ToString();
            }

            var originalShippingFee = vm.ShippingFee;
            ViewBag.OriginalShippingFee = originalShippingFee;

            var appliedCode = TempData["DiscountCode"]?.ToString();
            if (string.IsNullOrEmpty(appliedCode))
            {
                if (userId != null)
                {
                    var orderCount = await _context.Orders.CountAsync(o => o.UserId == userId);
                    if (orderCount == 0)
                    {
                        string autoCode = cart.Subtotal >= 500000 ? "NEWUSER200K" : "NEWUSERFREE";
                        var discount = await _discounts.GetActiveByCodeAsync(autoCode, cart.Subtotal);
                        if (discount != null)
                        {
                            appliedCode = autoCode;
                            TempData["DiscountCode"] = autoCode;

                            var discountAmount = discount.DiscountType == DiscountType.Percent
                                ? Math.Round(cart.Subtotal * discount.Value / 100, 0)
                                : discount.Value;

                            if (discount.PromotionType == PromotionType.FreeShipping)
                            {
                                discountAmount = 0m;
                                vm.ShippingFee = 0m;
                            }
                            else
                            {
                                discountAmount = Math.Min(discountAmount, cart.Subtotal);
                            }

                            HttpContext.Session.SetString("AppliedCouponCode", autoCode);
                            HttpContext.Session.SetString("DiscountAmount", discountAmount.ToString());
                        }
                    }
                }
            }

            if (!string.IsNullOrEmpty(appliedCode))
            {
                var discount = await _discounts.GetActiveByCodeAsync(appliedCode, cart.Subtotal);
                if (discount != null && discount.PromotionType == PromotionType.FreeShipping)
                {
                    vm.ShippingFee = 0m;
                }
            }

            TempData.Keep();
        }

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Shipping(CheckoutViewModel vm)
    {
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

        var shippingFee = CalculateShippingFee(vm.Shipping.ShippingAddress);
        TempData["ShippingFee"] = shippingFee.ToString();

        TempData["ShippingFullName"] = vm.Shipping.FullName;
        TempData["ShippingPhone"] = vm.Shipping.Phone;
        TempData["ShippingAddress"] = vm.Shipping.ShippingAddress;
        TempData["ShippingNotes"] = vm.Shipping.Notes;
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

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var subtotal = cart.Subtotal;

        // Apply discount
        var discountCode = TempData["DiscountCode"]?.ToString();
        var discountAmount = 0m;
        var shippingFee = TempData["ShippingFee"] is string feeStr && decimal.TryParse(feeStr, out var fee) ? fee : 50000m;

        if (!string.IsNullOrEmpty(discountCode))
        {
            var discount = await _discounts.GetActiveByCodeAsync(discountCode, subtotal);
            if (discount != null)
            {
                if (discount.PromotionType == Models.PromotionType.FirstOrder)
                {
                    var hasOrders = await _orders.UserHasOrdersAsync(userId);
                    if (hasOrders)
                    {
                        discount = null;
                        discountCode = null;
                        TempData["Error"] = "Mã ưu đãi đơn đầu tiên chỉ dành cho khách hàng mới.";
                    }
                }

                if (discount != null)
                {
                    if (discount.PromotionType == Models.PromotionType.FreeShipping)
                    {
                        shippingFee = 0m;
                    }
                    else
                    {
                        discountAmount = discount.DiscountType == DiscountType.Percent
                            ? Math.Round(subtotal * discount.Value / 100, 0)
                            : discount.Value;

                        if (discount.MaxDiscountAmount.HasValue)
                            discountAmount = Math.Min(discountAmount, discount.MaxDiscountAmount.Value);

                        discountAmount = Math.Min(discountAmount, subtotal);
                    }
                    discount.UsedCount++;
                    _discounts.Update(discount);
                }
            }
            else
            {
                discountCode = null;
            }
        }

        var total = subtotal - discountAmount + shippingFee;

        var order = new Order
        {
            UserId = userId,
            FullName = TempData["ShippingFullName"]?.ToString() ?? User.Identity?.Name ?? "",
            Phone = TempData["ShippingPhone"]?.ToString() ?? "",
            ShippingAddress = TempData["ShippingAddress"]?.ToString() ?? "",
            Notes = TempData["ShippingNotes"]?.ToString(),
            PaymentMethod = model.PaymentMethod,
            PaymentStatus = model.PaymentMethod == "VNPay" ? PaymentStatus.Pending : PaymentStatus.COD,
            DiscountCode = discountCode,
            DiscountAmount = discountAmount,
            Subtotal = subtotal,
            ShippingFee = shippingFee,
            Total = total,
            Status = OrderStatus.Pending,
            Items = cart.Items.Select(i => new OrderItem
            {
                ProductId = i.ProductId,
                ProductName = i.Name,
                UnitPrice = i.UnitPrice,
                Quantity = i.Quantity
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

        // ── COD: Xử lý xong, xóa giỏ hàng, bắn thông báo và gửi mail ngay ──
        if (model.PaymentMethod != "VNPay")
        {
            await _cartService.ClearCartAsync();
            HttpContext.Session.Remove("AppliedCouponCode");
            HttpContext.Session.Remove("DiscountAmount");

            try
            {
                // 1. GỬI EMAIL XÁC NHẬN ĐƠN HÀNG BẤT ĐỒNG BỘ CHO KHÁCH (COD)
                var customerEmail = User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name ?? "";
                if (!string.IsNullOrEmpty(customerEmail) && customerEmail.Contains("@"))
                {
                    _ = Task.Run(() => _emailService.SendOrderEmailAsync(customerEmail, order));
                }

                // 2. PHÁT TÍN HIỆU THÔNG BÁO ĐƠN HÀNG MỚI REAL-TIME CHO ADMIN (COD)
                await _hubContext.Clients.All.SendAsync("ReceiveNewOrderNotification", order.Id, order.FullName, order.Total);
            }
            catch (Exception ex)
            {
                // Ghi log lỗi nền nếu hệ thống mạng nghẽn, tránh làm gián đoạn luồng Checkout của khách
                Console.WriteLine($"Lỗi gửi thông báo hệ thống: {ex.Message}");
            }

            return RedirectToAction(nameof(Confirmation), new { id = order.Id });
        }

        // ── VNPay: chuyển hướng sang cổng thanh toán ──
        var returnUrl = _config["VnPay:ReturnUrl"] is { Length: > 0 } u
                       ? u
                       : Url.Action("VnPayReturn", "Checkout", null, Request.Scheme)!;
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
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
            order.PaymentStatus = PaymentStatus.Paid;
            order.VnPayTransactionId = result.TransactionId;
            _context.Orders.Update(order);
            await _context.SaveChangesAsync();
            await _cartService.ClearCartAsync();
            HttpContext.Session.Remove("AppliedCouponCode");
            HttpContext.Session.Remove("DiscountAmount");

            TempData["Success"] = $"Thanh toán thành công! Mã giao dịch: {result.TransactionId}";

            try
            {
                // 3. GỬI EMAIL XÁC NHẬN ĐƠN HÀNG CHO KHÁCH (VNPay Thành công)
                var customerEmail = User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name ?? "";
                if (!string.IsNullOrEmpty(customerEmail) && customerEmail.Contains("@"))
                {
                    _ = Task.Run(() => _emailService.SendOrderEmailAsync(customerEmail, order));
                }

                // 4. PHÁT TÍN HIỆU THÔNG BÁO REAL-TIME CHO ADMIN (VNPay Thành công)
                await _hubContext.Clients.All.SendAsync("ReceiveNewOrderNotification", order.Id, order.FullName, order.Total);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi gửi thông báo hệ thống: {ex.Message}");
            }
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
        if (string.IsNullOrWhiteSpace(req.Code))
        {
            HttpContext.Session.Remove("AppliedCouponCode");
            HttpContext.Session.Remove("DiscountAmount");
            TempData.Remove("DiscountCode");
            return Json(new { success = true, code = "", discountAmount = 0m, message = "Đã bỏ áp dụng mã giảm giá." });
        }

        var cart = await _cartService.GetCartAsync();
        var discount = await _discounts.GetActiveByCodeAsync(req.Code, cart.Subtotal);

        if (discount == null)
        {
            HttpContext.Session.Remove("AppliedCouponCode");
            HttpContext.Session.Remove("DiscountAmount");
            TempData.Remove("DiscountCode");
            return Json(new { success = false, message = "Mã giảm giá không hợp lệ hoặc không áp dụng được." });
        }

        // Kiểm tra điều kiện đơn hàng đầu tiên (FirstOrder)
        if (discount.PromotionType == PromotionType.FirstOrder)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId != null)
            {
                var hasOrders = await _orders.UserHasOrdersAsync(userId);
                if (hasOrders)
                {
                    HttpContext.Session.Remove("AppliedCouponCode");
                    HttpContext.Session.Remove("DiscountAmount");
                    TempData.Remove("DiscountCode");
                    return Json(new { success = false, message = "Mã ưu đãi đơn đầu tiên chỉ dành cho khách hàng mới." });
                }
            }
        }

        var discountAmount = discount.DiscountType == DiscountType.Percent
            ? Math.Round(cart.Subtotal * discount.Value / 100, 0)
            : discount.Value;

        if (discount.PromotionType == PromotionType.FreeShipping)
        {
            discountAmount = 0m;
        }
        else
        {
            discountAmount = Math.Min(discountAmount, cart.Subtotal);
        }

        TempData["DiscountCode"] = discount.Code;
        TempData.Keep("DiscountCode");

        HttpContext.Session.SetString("AppliedCouponCode", discount.Code);
        HttpContext.Session.SetString("DiscountAmount", discountAmount.ToString());

        return Json(new
        {
            success = true,
            code = discount.Code,
            discountAmount,
            isFreeShipping = discount.PromotionType == PromotionType.FreeShipping,
            message = $"Áp dụng thành công: {discount.Name}"
        });
    }

    public async Task<IActionResult> Confirmation(int id)
    {
        var order = await _orders.GetWithItemsAsync(id);
        if (order == null)
            return NotFound();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId != null && order.UserId != userId)
            return NotFound();

        return View(new OrderConfirmationViewModel { Order = order });
    }
}