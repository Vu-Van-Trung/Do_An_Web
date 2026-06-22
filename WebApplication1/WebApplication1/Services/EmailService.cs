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

        public async Task SendPasswordResetEmailAsync(string emailAddress, string callbackUrl)
        {
            var smtpServer = _configuration["EmailSettings:SmtpServer"] ?? "smtp.gmail.com";
            var port = int.Parse(_configuration["EmailSettings:Port"] ?? "587");
            var senderName = _configuration["EmailSettings:SenderName"] ?? "NexusGear";
            var senderEmail = _configuration["EmailSettings:SenderEmail"] ?? "";
            var username = _configuration["EmailSettings:Username"] ?? "";
            var password = _configuration["EmailSettings:Password"] ?? "";

            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(senderName, senderEmail));
            email.To.Add(MailboxAddress.Parse(emailAddress));
            email.Subject = $"[NexusGear] Yêu cầu khôi phục mật khẩu tài khoản";

            var bodyBuilder = new BodyBuilder();
            bodyBuilder.HtmlBody = $@"
                <div style='font-family: ""Segoe UI"", Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #e2e8f0; background-color: #0f172a; padding: 2rem; border-radius: 16px; max-width: 600px; margin: 0 auto; border: 1px solid #1e293b;'>
                    <div style='text-align: center; margin-bottom: 2rem;'>
                        <h1 style='color: #8b5cf6; font-size: 24px; margin: 0; font-weight: 700; letter-spacing: 0.5px;'>NEXUS GEAR</h1>
                        <p style='color: #94a3b8; font-size: 14px; margin: 5px 0 0 0;'>Your Ultimate Gaming Gear Hub</p>
                    </div>
                    
                    <div style='background-color: #1e293b; padding: 1.5rem; border-radius: 12px; border: 1px solid #334155;'>
                        <h2 style='color: #f1f5f9; font-size: 18px; margin-top: 0; margin-bottom: 1rem;'>Yêu cầu đặt lại mật khẩu</h2>
                        <p style='color: #cbd5e1; font-size: 15px;'>Xin chào,</p>
                        <p style='color: #cbd5e1; font-size: 15px;'>Chúng tôi nhận được yêu cầu khôi phục mật khẩu cho tài khoản liên kết với địa chỉ email này. Để tiến hành đặt lại mật khẩu mới, vui lòng nhấn vào nút bên dưới:</p>
                        
                        <div style='text-align: center; margin: 2rem 0;'>
                            <a href='{callbackUrl}' style='display: inline-block; background: linear-gradient(135deg, #8b5cf6 0%, #6366f1 100%); color: #ffffff; text-decoration: none; padding: 12px 30px; font-weight: bold; border-radius: 8px; box-shadow: 0 4px 12px rgba(139, 92, 246, 0.3); font-size: 15px;'>Đặt lại mật khẩu</a>
                        </div>
                        
                        <p style='color: #94a3b8; font-size: 13px; line-height: 1.5;'>Liên kết khôi phục mật khẩu này sẽ hết hạn sau một khoảng thời gian nhất định vì lý do bảo mật.</p>
                        <p style='color: #94a3b8; font-size: 13px; margin-bottom: 0;'>Nếu bạn không thực hiện yêu cầu này, vui lòng bỏ qua email này hoặc liên hệ bộ phận hỗ trợ nếu nghi ngờ tài khoản bị xâm nhập.</p>
                    </div>
                    
                    <div style='margin-top: 2rem; border-top: 1px solid #1e293b; padding-top: 1.5rem; text-align: center;'>
                        <p style='color: #64748b; font-size: 13px; margin: 0;'>Nếu nút trên không hoạt động, bạn có thể sao chép liên kết dưới đây vào trình duyệt:</p>
                        <p style='color: #cbd5e1; font-size: 12px; word-break: break-all; margin: 8px 0 0 0;'>
                            <a href='{callbackUrl}' style='color: #8b5cf6; text-decoration: underline;'>{callbackUrl}</a>
                        </p>
                    </div>
                    
                    <hr style='border: none; border-top: 1px solid #1e293b; margin: 2rem 0;' />
                    <p style='font-size: 11px; color: #64748b; text-align: center; margin: 0;'>Đây là email tự động từ hệ thống NexusGear. Vui lòng không trả lời thư này.</p>
                </div>";

            email.Body = bodyBuilder.ToMessageBody();

            using var smtp = new SmtpClient();
            try
            {
                await smtp.ConnectAsync(smtpServer, port, SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(username, password);
                await smtp.SendAsync(email);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi gửi email reset: {ex.Message}");
                throw;
            }
            finally
            {
                await smtp.DisconnectAsync(true);
            }
        }
    }
}