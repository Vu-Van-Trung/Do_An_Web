using WebApplication1.Models;

namespace WebApplication1.Services;

public interface IVnPayService
{
    string CreatePaymentUrl(Order order, string returnUrl, string clientIp);
    VnPayReturnModel ProcessReturn(IQueryCollection query);
}

public class VnPayReturnModel
{
    public bool IsValidSignature { get; set; }
    public bool IsSuccess { get; set; }
    public int OrderId { get; set; }
    public string TransactionId { get; set; } = string.Empty;
    public string ResponseCode { get; set; } = string.Empty;
    public string Message => ResponseCode switch
    {
        "00" => "Giao dịch thành công",
        "07" => "Giao dịch bị nghi ngờ (lừa đảo, bất thường)",
        "09" => "Thẻ/Tài khoản chưa đăng ký InternetBanking",
        "10" => "Xác thực thông tin thẻ/tài khoản sai quá 3 lần",
        "11" => "Đã hết hạn chờ thanh toán. Vui lòng thực hiện lại",
        "12" => "Thẻ/Tài khoản bị khóa",
        "13" => "Nhập sai OTP quá số lần quy định",
        "24" => "Khách hàng hủy giao dịch",
        "51" => "Tài khoản không đủ số dư",
        "65" => "Vượt quá hạn mức giao dịch trong ngày",
        "75" => "Ngân hàng đang bảo trì",
        "79" => "Nhập sai mật khẩu thanh toán quá số lần quy định",
        _ => "Giao dịch thất bại"
    };
}
