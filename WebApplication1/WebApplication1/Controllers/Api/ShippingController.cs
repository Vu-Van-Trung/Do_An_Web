using Microsoft.AspNetCore.Mvc;
using WebApplication1.Dtos;
using WebApplication1.Services;

namespace WebApplication1.Controllers.Api;

[ApiController]
[Route("api/shipping")]
[Produces("application/json")]
public class ShippingController : ControllerBase
{
    /// <summary>
    /// Lấy danh sách tùy chọn giao hàng theo tỉnh/thành và loại hàng hóa.
    /// shippingClass: Nho | Vua | CongKenh
    /// </summary>
    [HttpGet("options")]
    public ActionResult<List<ShippingOptionDto>> GetOptions(
        [FromQuery] int provinceCode,
        [FromQuery] string shippingClass = "Nho")
    {
        if (provinceCode <= 0)
            return BadRequest(new { message = "Vui lòng cung cấp provinceCode hợp lệ." });

        if (!Enum.TryParse<ShippingClass>(shippingClass, ignoreCase: true, out var cls))
            cls = ShippingClass.Nho;

        var options = ShippingCalculator.CalculateShippingFee(provinceCode, cls);
        return Ok(options.Select(o => new ShippingOptionDto(
            o.Service.ToString(), o.ServiceName, o.Fee, o.Eta, o.IsContactOnly)));
    }

    /// <summary>Danh sách vùng và tỉnh/thành phố được hỗ trợ</summary>
    [HttpGet("regions")]
    public IActionResult GetRegions()
    {
        return Ok(new[]
        {
            new { region = "NoiThanhHCM",  label = "Nội thành TP.HCM", provinceCodes = new[] { 79 } },
            new { region = "MienNam",       label = "Miền Nam",          provinceCodes = new[] { 75,80,82,86,91,92,96 } },
            new { region = "MienTrung",     label = "Miền Trung",        provinceCodes = new[] { 38,40,42,44,46,48,51,52,56,66,68 } },
            new { region = "MienBac",       label = "Miền Bắc",          provinceCodes = new[] { 1,4,8,11,12,14,15,19,20,22,24,25,31,33,37 } },
        });
    }
}
