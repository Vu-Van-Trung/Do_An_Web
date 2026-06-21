using Microsoft.AspNetCore.Mvc;
using WebApplication1.Dtos;
using WebApplication1.Models;
using WebApplication1.Repositories;

namespace WebApplication1.Controllers.Api;

[ApiController]
[Route("api/discounts")]
[Produces("application/json")]
public class DiscountsController : ControllerBase
{
    private readonly IDiscountRepository _discounts;
    public DiscountsController(IDiscountRepository discounts) => _discounts = discounts;

    /// <summary>Kiểm tra và tính toán giá trị mã giảm giá</summary>
    [HttpPost("validate")]
    public async Task<ActionResult<DiscountResultDto>> Validate(ValidateDiscountRequest req)
    {
        var code     = req.Code.Trim().ToUpper();
        var discount = await _discounts.GetActiveByCodeAsync(code, req.OrderSubtotal);

        if (discount == null)
            return Ok(new DiscountResultDto(
                false, "Mã giảm giá không hợp lệ hoặc đã hết hạn/số lần dùng.",
                0, false, null, null));

        var isFreeShip = discount.PromotionType == PromotionType.FreeShipping;
        var amount     = isFreeShip ? 0m
            : discount.DiscountType == DiscountType.Percent
                ? Math.Round(req.OrderSubtotal * discount.Value / 100, 0)
                : discount.Value;

        if (!isFreeShip && discount.MaxDiscountAmount.HasValue)
            amount = Math.Min(amount, discount.MaxDiscountAmount.Value);
        if (!isFreeShip)
            amount = Math.Min(amount, req.OrderSubtotal);

        return Ok(new DiscountResultDto(
            true,
            $"Áp dụng thành công: {discount.Name}",
            amount, isFreeShip,
            discount.Code, discount.Name));
    }
}
