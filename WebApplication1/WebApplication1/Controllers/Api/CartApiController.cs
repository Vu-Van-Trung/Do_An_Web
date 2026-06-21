using Microsoft.AspNetCore.Mvc;
using WebApplication1.Dtos;
using WebApplication1.Services;

namespace WebApplication1.Controllers.Api;

[ApiController]
[Route("api/cart")]
[Produces("application/json")]
public class CartApiController : ControllerBase
{
    private readonly ICartService _cart;
    public CartApiController(ICartService cart) => _cart = cart;

    /// <summary>Lấy giỏ hàng hiện tại (session hoặc tài khoản)</summary>
    [HttpGet]
    public async Task<ActionResult<CartResponseDto>> GetCart()
    {
        var cart = await _cart.GetCartAsync();
        return Ok(new CartResponseDto(
            cart.Items.Select(i => new CartItemDto(
                i.CartItemId, i.ProductId, i.Name, i.ImageUrl,
                i.UnitPrice, i.Quantity, i.Stock, i.LineTotal,
                i.ShippingClass.ToString())).ToList(),
            cart.Subtotal,
            cart.ItemCount));
    }

    /// <summary>Thêm sản phẩm vào giỏ hàng</summary>
    [HttpPost("items")]
    public async Task<IActionResult> AddItem(AddToCartRequest req)
    {
        try
        {
            await _cart.AddToCartAsync(req.ProductId, req.Quantity);
            var cart = await _cart.GetCartAsync();
            return Ok(new { message = "Đã thêm vào giỏ hàng.", itemCount = cart.ItemCount });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>Cập nhật số lượng — số lượng = 0 sẽ xóa sản phẩm</summary>
    [HttpPut("items/{cartItemId:int}")]
    public async Task<IActionResult> UpdateItem(int cartItemId, UpdateCartItemRequest req)
    {
        await _cart.UpdateQuantityAsync(cartItemId, req.Quantity);
        var cart = await _cart.GetCartAsync();
        return Ok(new { message = "Đã cập nhật.", subtotal = cart.Subtotal, itemCount = cart.ItemCount });
    }

    /// <summary>Xóa một sản phẩm khỏi giỏ hàng</summary>
    [HttpDelete("items/{cartItemId:int}")]
    public async Task<IActionResult> RemoveItem(int cartItemId)
    {
        await _cart.RemoveItemAsync(cartItemId);
        var cart = await _cart.GetCartAsync();
        return Ok(new { message = "Đã xóa sản phẩm.", subtotal = cart.Subtotal, itemCount = cart.ItemCount });
    }

    /// <summary>Xóa toàn bộ giỏ hàng</summary>
    [HttpDelete]
    public async Task<IActionResult> ClearCart()
    {
        await _cart.ClearCartAsync();
        return Ok(new { message = "Đã xóa toàn bộ giỏ hàng." });
    }
}
