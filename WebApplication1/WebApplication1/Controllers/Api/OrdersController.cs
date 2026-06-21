using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Dtos;
using WebApplication1.Models;
using WebApplication1.Repositories;
using WebApplication1.Services;

namespace WebApplication1.Controllers.Api;

[ApiController]
[Route("api/orders")]
[Produces("application/json")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ICartService          _cart;
    private readonly IOrderRepository      _orders;
    private readonly IDiscountRepository   _discounts;

    public OrdersController(ApplicationDbContext db, ICartService cart,
        IOrderRepository orders, IDiscountRepository discounts)
    {
        _db        = db;
        _cart      = cart;
        _orders    = orders;
        _discounts = discounts;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    /// <summary>Danh sách đơn hàng của tài khoản đang đăng nhập</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<OrderListDto>>> GetOrders(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        pageSize = Math.Clamp(pageSize, 1, 50);
        page     = Math.Max(1, page);

        var q = _db.Orders
            .Include(o => o.Items)
            .Where(o => o.UserId == UserId)
            .OrderByDescending(o => o.OrderDate);

        var total = await q.CountAsync();
        var items = await q
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(o => new OrderListDto(
                o.Id, o.OrderDate, o.Status.ToString(), o.PaymentMethod,
                o.PaymentStatus.ToString(), o.Subtotal, o.ShippingFee,
                o.DiscountAmount, o.Total, o.Items.Count))
            .ToListAsync();

        return Ok(new PagedResult<OrderListDto>(
            items, page, pageSize, total,
            (int)Math.Ceiling(total / (double)pageSize)));
    }

    /// <summary>Chi tiết đơn hàng</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderDetailDto>> GetById(int id)
    {
        var o = await _db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id && o.UserId == UserId);

        if (o == null) return NotFound(new { message = "Đơn hàng không tồn tại." });

        return Ok(new OrderDetailDto(
            o.Id, o.OrderDate, o.Status.ToString(), o.PaymentMethod,
            o.PaymentStatus.ToString(), o.FullName, o.Phone, o.ShippingAddress, o.Notes,
            o.Subtotal, o.ShippingFee, o.DiscountAmount, o.DiscountCode, o.Total,
            o.Items.Select(i => new OrderItemDto(
                i.ProductId, i.ProductName, i.UnitPrice, i.Quantity, i.LineTotal)).ToList()));
    }

    /// <summary>Đặt hàng mới</summary>
    [HttpPost]
    public async Task<IActionResult> PlaceOrder(PlaceOrderApiRequest req)
    {
        var cartVm = req.SelectedCartItemIds?.Length > 0
            ? await _cart.GetSelectedCartAsync(req.SelectedCartItemIds)
            : await _cart.GetCartAsync();

        if (!cartVm.Items.Any())
            return BadRequest(new { message = "Giỏ hàng trống." });

        if (!Enum.TryParse<ShippingService>(req.ShippingService, true, out var svc))
            svc = ShippingService.Nhanh;

        var dominantClass = cartVm.Items
            .Select(i => i.ShippingClass)
            .OrderByDescending(s => (int)s)
            .First();

        var options     = ShippingCalculator.CalculateShippingFee(req.ProvinceCode, dominantClass);
        var chosen      = options.FirstOrDefault(o => o.Service == svc && !o.IsContactOnly)
                       ?? options.FirstOrDefault(o => !o.IsContactOnly);
        var shippingFee = chosen?.Fee ?? 0m;

        var subtotal       = cartVm.Subtotal;
        var discountAmount = 0m;
        var discountCode   = req.DiscountCode?.Trim().ToUpper();

        if (!string.IsNullOrEmpty(discountCode))
        {
            var discount = await _discounts.GetActiveByCodeAsync(discountCode, subtotal);
            if (discount != null)
            {
                if (discount.PromotionType == PromotionType.FreeShipping)
                    shippingFee = 0m;
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
            else discountCode = null;
        }

        var order = new Order
        {
            UserId          = UserId,
            FullName        = req.FullName,
            Phone           = req.Phone,
            ShippingAddress = req.ShippingAddress,
            Notes           = req.Notes,
            PaymentMethod   = req.PaymentMethod,
            PaymentStatus   = req.PaymentMethod == "COD" ? PaymentStatus.COD : PaymentStatus.Pending,
            DiscountCode    = discountCode,
            DiscountAmount  = discountAmount,
            Subtotal        = subtotal,
            ShippingFee     = shippingFee,
            Total           = subtotal - discountAmount + shippingFee,
            Status          = OrderStatus.Pending,
            Items = cartVm.Items.Select(i => new OrderItem
            {
                ProductId   = i.ProductId,
                ProductName = i.Name,
                UnitPrice   = i.UnitPrice,
                Quantity    = i.Quantity
            }).ToList()
        };

        foreach (var line in cartVm.Items)
        {
            var product = await _db.Products.FindAsync(line.ProductId);
            if (product == null || product.Stock < line.Quantity)
                return BadRequest(new { message = $"Sản phẩm \"{line.Name}\" không đủ hàng trong kho." });
            product.Stock -= line.Quantity;
        }

        await _orders.AddAsync(order);
        await _orders.SaveChangesAsync();

        if (req.PaymentMethod != "VNPay")
            await _cart.ClearCartAsync();

        return Created($"/api/orders/{order.Id}", new
        {
            orderId       = order.Id,
            total         = order.Total,
            paymentMethod = order.PaymentMethod,
            message       = "Đặt hàng thành công!"
        });
    }

    /// <summary>Hủy đơn hàng (chỉ khi trạng thái Pending)</summary>
    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id)
    {
        var order = await _db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id && o.UserId == UserId);

        if (order == null) return NotFound(new { message = "Đơn hàng không tồn tại." });
        if (order.Status != OrderStatus.Pending)
            return BadRequest(new { message = "Chỉ có thể hủy đơn hàng đang ở trạng thái Chờ xử lý." });

        order.Status = OrderStatus.Cancelled;
        foreach (var item in order.Items)
        {
            var product = await _db.Products.FindAsync(item.ProductId);
            if (product != null) product.Stock += item.Quantity;
        }
        await _db.SaveChangesAsync();

        return Ok(new { message = "Đơn hàng đã được hủy." });
    }
}
