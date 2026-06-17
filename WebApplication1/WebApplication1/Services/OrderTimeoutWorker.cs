using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Workers
{
    public class OrderTimeoutWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<OrderTimeoutWorker> _logger;

        public OrderTimeoutWorker(IServiceProvider serviceProvider, ILogger<OrderTimeoutWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("OrderTimeoutWorker đã bắt đầu khởi chạy.");

            // Vòng lặp chạy vô hạn cho đến khi ứng dụng bị tắt đi (stoppingToken được kích hoạt)
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                        // Định mốc thời gian: Lấy thời điểm hiện tại lùi lại 20 phút
                        var timeoutLimit = DateTime.UtcNow.AddMinutes(-20);

                        // Tìm các đơn hàng thỏa mãn: Đang chờ thanh toán VÀ tạo trước mốc 20 phút vừa tính
                        // Đã sửa o.OrderItems -> o.Items | o.CreatedAt -> o.OrderDate
                        var abandonedOrders = await dbContext.Orders
                            .Include(o => o.Items)
                            .Where(o => o.PaymentStatus == PaymentStatus.Pending && o.OrderDate <= timeoutLimit)
                            .ToListAsync(stoppingToken);

                        if (abandonedOrders.Any())
                        {
                            _logger.LogInformation($"Tìm thấy {abandonedOrders.Count} đơn hàng quá hạn thanh toán. Tiến hành hủy và hoàn kho.");

                            foreach (var order in abandonedOrders)
                            {
                                // Cập nhật trạng thái đơn hàng và thanh toán thành Thất bại/Hủy
                                // Đã sửa order.OrderStatus -> order.Status
                                order.PaymentStatus = PaymentStatus.Failed;
                                order.Status = OrderStatus.Cancelled; // Lưu ý: Nếu Enum của bạn viết là Canceled (1 chữ l) thì sửa lại cho đúng nhé

                                // Duyệt qua từng sản phẩm trong đơn hàng để cộng trả lại kho số lượng (Stock)
                                // Đã sửa order.OrderItems -> order.Items
                                foreach (var item in order.Items)
                                {
                                    var product = await dbContext.Products.FindAsync(new object[] { item.ProductId }, stoppingToken);
                                    if (product != null)
                                    {
                                        product.Stock += item.Quantity;
                                        _logger.LogInformation($"Đã hoàn trả {item.Quantity} sản phẩm (ID: {product.Id}) về kho.");
                                    }
                                }
                            }

                            // Lưu toàn bộ thay đổi vào cơ sở dữ liệu
                            await dbContext.SaveChangesAsync(stoppingToken);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Có lỗi xảy ra khi quét đơn hàng quá hạn.");
                }

                // Nghỉ 5 phút trước khi thực hiện lần quét tiếp theo
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }
}