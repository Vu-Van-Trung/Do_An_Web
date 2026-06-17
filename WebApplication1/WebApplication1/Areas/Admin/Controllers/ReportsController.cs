using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.ViewModels;

namespace WebApplication1.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,Manager")]
public class ReportsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public ReportsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // 1. CẬP NHẬT: Thêm tham số lọc fromDate và toDate từ giao diện gửi về
    public async Task<IActionResult> Index(DateTime? fromDate, DateTime? toDate)
    {
        var now = DateTime.UtcNow;
        var firstOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        // Chuẩn bị truy vấn cho Đơn hàng (Bỏ qua đơn hủy)
        var ordersQuery = _context.Orders
            .Include(o => o.Items)
            .Where(o => o.Status != OrderStatus.Cancelled);

        // Chuẩn bị truy vấn cho OrderItems bán chạy
        var orderItemsQuery = _context.OrderItems
            .Include(oi => oi.Order)
            .Where(oi => oi.Order.Status != OrderStatus.Cancelled);

        // Chuẩn bị truy vấn đếm tổng quan trạng thái
        var allOrdersQuery = _context.Orders.AsQueryable();

        bool isFiltered = fromDate.HasValue || toDate.HasValue;

        // Nếu có truyền bộ lọc khoảng ngày, tiến hành ép điều kiện lọc thời gian
        if (isFiltered)
        {
            var start = fromDate ?? DateTime.MinValue;
            var end = toDate ?? DateTime.MaxValue;
            // Đảm bảo quét hết dữ liệu đến cuối ngày của mốc kết thúc (23:59:59)
            var endOfEndDay = end.Date.AddDays(1).AddTicks(-1);

            ordersQuery = ordersQuery.Where(o => o.OrderDate >= start && o.OrderDate <= endOfEndDay);
            orderItemsQuery = orderItemsQuery.Where(oi => oi.Order.OrderDate >= start && oi.Order.OrderDate <= endOfEndDay);
            allOrdersQuery = allOrdersQuery.Where(o => o.OrderDate >= start && o.OrderDate <= endOfEndDay);
        }

        // Thực thi lấy dữ liệu từ Database sau khi đã qua bộ lọc
        var orders = await ordersQuery.ToListAsync();
        var allOrders = await allOrdersQuery.ToListAsync();
        var customers = await _userManager.GetUsersInRoleAsync("Customer");

        List<MonthlyRevenue> monthlyRevenues;

        if (isFiltered)
        {
            // Nếu có bộ lọc: Gom nhóm doanh thu chi tiết theo từng Ngày để vẽ biểu đồ cho khít khoảng ngày chọn
            monthlyRevenues = orders
                .GroupBy(o => o.OrderDate.Date)
                .Select(g => new MonthlyRevenue
                {
                    Month = g.Key.ToString("dd/MM/yyyy"), // Tận dụng trường Month lưu chuỗi ngày hiển thị
                    Revenue = g.Sum(o => o.Total)
                })
                .OrderBy(g => DateTime.ParseExact(g.Month, "dd/MM/yyyy", null))
                .ToList();
        }
        else
        {
            // GIỮ NGUYÊN: Nếu không lọc ngày, chạy lại logic tính toán doanh thu 12 tháng gần nhất như cũ của bạn
            monthlyRevenues = Enumerable.Range(0, 12)
                .Select(i => now.AddMonths(-i))
                .Select(m => new MonthlyRevenue
                {
                    Month = m.ToString("MM/yyyy"),
                    Revenue = orders
                        .Where(o => o.OrderDate.Year == m.Year && o.OrderDate.Month == m.Month)
                        .Sum(o => o.Total)
                })
                .Reverse()
                .ToList();
        }

        // GIỮ NGUYÊN: Top sản phẩm bán chạy (nhưng có áp dụng bộ lọc khoảng ngày nếu có)
        var topProducts = await orderItemsQuery
            .GroupBy(oi => oi.ProductName)
            .Select(g => new TopProduct
            {
                Name = g.Key,
                TotalSold = g.Sum(oi => oi.Quantity),
                TotalRevenue = g.Sum(oi => oi.UnitPrice * oi.Quantity)
            })
            .OrderByDescending(x => x.TotalSold)
            .Take(10)
            .ToListAsync();

        var vm = new ReportViewModel
        {
            MonthlyRevenues = monthlyRevenues,
            TopProducts = topProducts,
            TotalOrders = allOrders.Count,
            TotalRevenue = orders.Sum(o => o.Total),
            CompletedOrders = allOrders.Count(o => o.Status == OrderStatus.Completed),
            CancelledOrders = allOrders.Count(o => o.Status == OrderStatus.Cancelled),
            NewCustomersThisMonth = customers.Count(u => u.CreatedAt >= firstOfMonth)
        };

        // Trả mốc ngày hiện tại về ViewBag để gán cứng vào ô hiển thị trên giao diện HTML
        ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
        ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");

        return View(vm);
    }

    // 2. BỔ SUNG: Action xử lý xuất toàn bộ danh sách dữ liệu báo cáo doanh thu ra file Excel/CSV tiếng Việt không lỗi font
    [HttpGet]
    public async Task<IActionResult> ExportRevenueCsv(DateTime? fromDate, DateTime? toDate)
    {
        var ordersQuery = _context.Orders.Where(o => o.Status != OrderStatus.Cancelled);

        if (fromDate.HasValue || toDate.HasValue)
        {
            var start = fromDate ?? DateTime.MinValue;
            var end = toDate ?? DateTime.MaxValue;
            var endOfEndDay = end.Date.AddDays(1).AddTicks(-1);
            ordersQuery = ordersQuery.Where(o => o.OrderDate >= start && o.OrderDate <= endOfEndDay);
        }

        var data = await ordersQuery
            .OrderByDescending(o => o.OrderDate)
            .Select(o => new { o.Id, o.OrderDate, o.FullName, o.Total, o.PaymentMethod, o.PaymentStatus })
            .ToListAsync();

        var csv = new StringBuilder();
        // Thêm ký tự BOM (\uFEFF) ở đầu file để ép Excel mở file bằng bảng mã UTF-8 không lỗi font tiếng Việt
        csv.AppendLine('\uFEFF' + "Mã Đơn Hàng;Ngày Đặt;Khách Hàng;Tổng Tiền (₫);Hình Thức Thanh Toán;Trạng Thái");

        foreach (var item in data)
        {
            csv.AppendLine($"{item.Id};{item.OrderDate:dd/MM/yyyy HH:mm};{item.FullName};{item.Total};{item.PaymentMethod};{item.PaymentStatus}");
        }

        var fileBytes = Encoding.UTF8.GetBytes(csv.ToString());
        string fileName = $"BaoCaoDoanhThu_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

        return File(fileBytes, "text/csv", fileName);
    }
}