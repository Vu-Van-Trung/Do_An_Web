namespace WebApplication1.Models;

public enum PaymentStatus
{
    Pending,   // Chờ thanh toán (VNPay chưa xác nhận)
    Paid,      // Đã thanh toán thành công
    Failed,    // Thanh toán thất bại
    COD        // Thanh toán khi nhận hàng
}
