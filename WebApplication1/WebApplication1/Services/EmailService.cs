using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using WebApplication1.Models;

namespace WebApplication1.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendOrderEmailAsync(string customerEmail, Order order)
        {
            // 1. Lấy thông tin cấu hình từ appsettings.json
            var smtpServer = _configuration["EmailSettings:SmtpServer"] ?? "smtp.gmail.com";
            var port = int.Parse(_configuration["EmailSettings:Port"] ?? "587");
            var senderName = _configuration["EmailSettings:SenderName"] ?? "NexusGear";
            var senderEmail = _configuration["EmailSettings:SenderEmail"] ?? "";
            var username = _configuration["EmailSettings:Username"] ?? "";
            var password = _configuration["EmailSettings:Password"] ?? "";

            // 2. Tạo nội dung Email bằng MimeKit
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(senderName, senderEmail));
            email.To.Add(MailboxAddress.Parse(customerEmail));
            email.Subject = $"[YourShop] Xác nhận đơn hàng thành công #{order.Id}";

            // Thiết kế nội dung HTML gửi cho khách hàng (Dựa trên các trường thực tế trong Order.cs của bạn)
            var bodyBuilder = new BodyBuilder();
            bodyBuilder.HtmlBody = $@"
                <div style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                    <h2 style='color: #4CAF50;'>Cảm ơn bạn đã mua hàng tại YourShop!</h2>
                    <p>Xin chào <strong>{order.FullName}</strong>,</p>
                    <p>Đơn hàng của bạn đã được tiếp nhận thành công. Dưới đây là thông tin chi tiết đơn hàng:</p>
                    
                    <table style='width: 100%; border-collapse: collapse; margin: 20px 0;'>
                        <tr style='background-color: #f2f2f2;'>
                            <th style='padding: 8px; border: 1px solid #ddd; text-align: left;'>Mục</th>
                            <th style='padding: 8px; border: 1px solid #ddd; text-align: left;'>Thông tin</th>
                        </tr>
                        <tr>
                            <td style='padding: 8px; border: 1px solid #ddd;'><strong>Mã đơn hàng:</strong></td>
                            <td style='padding: 8px; border: 1px solid #ddd;'>#{order.Id}</td>
                        </tr>
                        <tr>
                            <td style='padding: 8px; border: 1px solid #ddd;'><strong>Ngày đặt:</strong></td>
                            <td style='padding: 8px; border: 1px solid #ddd;'>{order.OrderDate.ToLocalTime():dd/MM/yyyy HH:mm}</td>
                        </tr>
                        <tr>
                            <td style='padding: 8px; border: 1px solid #ddd;'><strong>Phương thức thanh toán:</strong></td>
                            <td style='padding: 8px; border: 1px solid #ddd;'>{order.PaymentMethod} ({order.PaymentStatus})</td>
                        </tr>
                        <tr>
                            <td style='padding: 8px; border: 1px solid #ddd;'><strong>Địa chỉ giao hàng:</strong></td>
                            <td style='padding: 8px; border: 1px solid #ddd;'>{order.ShippingAddress}</td>
                        </tr>
                        <tr style='font-weight: bold; color: #e74c3c;'>
                            <td style='padding: 8px; border: 1px solid #ddd;'>Tổng tiền thanh toán:</td>
                            <td style='padding: 8px; border: 1px solid #ddd;'>{order.Total:N0} VNĐ</td>
                        </tr>
                    </table>

                    <p>Chúng tôi sẽ sớm liên hệ với bạn để xác nhận lộ trình giao hàng.</p>
                    <hr style='border: none; border-top: 1px solid #ddd;' />
                    <p style='font-size: 12px; color: #777;'>Đây là email tự động, vui lòng không phản hồi email này.</p>
                </div>";

            email.Body = bodyBuilder.ToMessageBody();

            // 3. Tiến hành kết nối SMTP Server và gửi Mail bằng MailKit
            using var smtp = new SmtpClient();
            try
            {
                // Kết nối tới máy chủ SMTP
                await smtp.ConnectAsync(smtpServer, port, SecureSocketOptions.StartTls);

                // Xác thực tài khoản
                await smtp.AuthenticateAsync(username, password);

                // Gửi email đi
                await smtp.SendAsync(email);
            }
            catch (Exception ex)
            {
                // Ghi log lỗi nếu không gửi được (ví dụ sai mật khẩu ứng dụng, nghẽn mạng)
                Console.WriteLine($"Lỗi gửi email: {ex.Message}");
                throw;
            }
            finally
            {
                // Ngắt kết nối an toàn
                await smtp.DisconnectAsync(true);
            }
        }
    }
}