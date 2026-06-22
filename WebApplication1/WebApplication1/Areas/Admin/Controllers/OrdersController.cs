using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebApplication1.Models;
using WebApplication1.Repositories;

namespace WebApplication1.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "XemDonHang")]
public class OrdersController : Controller
{
    private readonly IOrderRepository _orders;

    public OrdersController(IOrderRepository orders)
    {
        _orders = orders;
    }

    public async Task<IActionResult> Index() =>
        View(await _orders.GetAllWithItemsAsync());

    public async Task<IActionResult> Details(int id)
    {
        var order = await _orders.GetWithItemsAsync(id);
        if (order == null) return NotFound();
        return View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "CapNhatTrangThaiDon")]
    public async Task<IActionResult> UpdateStatus(int id, OrderStatus status)
    {
        var order = await _orders.GetByIdAsync(id);
        if (order == null) return NotFound();
        order.Status = status;
        _orders.Update(order);
        await _orders.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id });
    }

    // ==========================================
    // BỔ SUNG: Action xuất dữ liệu toàn bộ đơn hàng ra file Excel/CSV chuẩn tiếng Việt
    // ==========================================
    [HttpGet]
    public async Task<IActionResult> ExportOrdersCsv()
    {
        // Sử dụng hàm có sẵn của bạn để lấy toàn bộ danh sách đơn hàng kèm các Item bên trong
        var allOrders = await _orders.GetAllWithItemsAsync();

        // Sắp xếp đơn hàng mới nhất lên trên đầu file xuất ra
        var sortedOrders = allOrders.OrderByDescending(o => o.Id);

        var csv = new StringBuilder();

        // Ký tự đặc biệt '\uFEFF' (BOM) bắt buộc đứng đầu file để ép Microsoft Excel nhận diện mã hóa UTF-8,
        // giúp hiển thị đúng dấu tiếng Việt (tên khách hàng, địa chỉ) không bị lỗi font ô vuông.
        csv.AppendLine('\uFEFF' + "Mã Đơn;Ngày Đặt;Khách Hàng;Số Điện Thoại;Địa Chỉ Giao Hàng;Tổng Tiền (₫);Phương Thức;Thanh Toán;Trạng Thái Đơn");

        foreach (var o in sortedOrders)
        {
            // Thay thế tất cả dấu xuống dòng (nếu có) trong trường địa chỉ nhận hàng để tránh làm vỡ định dạng hàng của file Excel
            var cleanAddress = o.ShippingAddress?.Replace("\r", " ").Replace("\n", " ").Replace(";", ",") ?? "";

            csv.AppendLine($"{o.Id};{o.OrderDate:dd/MM/yyyy HH:mm};{o.FullName};{o.Phone};{cleanAddress};{o.Total};{o.PaymentMethod};{o.PaymentStatus};{o.Status}");
        }

        // Đóng gói chuỗi text thành byte dữ liệu theo chuẩn mã hóa UTF-8
        var fileBytes = Encoding.UTF8.GetBytes(csv.ToString());

        // Đặt tên file động kèm theo ngày giờ xuất file để tránh trùng lặp tệp tin tải về
        string fileName = $"DanhSachDonHang_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

        // Trả file về trình duyệt Client dưới dạng text/csv để tự động kích hoạt tiến trình download
        return File(fileBytes, "text/csv", fileName);
    }
}