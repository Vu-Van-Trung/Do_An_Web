namespace WebApplication1.Models;

public static class AppPermissions
{
    public const string XemDashboard = "XemDashboard";
    public const string QuanLySanPham = "QuanLySanPham";
    public const string QuanLyDanhMuc = "QuanLyDanhMuc";
    public const string QuanLyThuongHieu = "QuanLyThuongHieu";
    public const string XemDonHang = "XemDonHang";
    public const string CapNhatTrangThaiDon = "CapNhatTrangThaiDon";
    public const string QuanLyKhuyenMai = "QuanLyKhuyenMai";
    public const string XemBaoCaoDoanhThu = "XemBaoCaoDoanhThu";
    public const string QuanLyNguoiDung = "QuanLyNguoiDung";
    public const string PhanQuyenVaiTro = "PhanQuyenVaiTro";
    public const string DatHang = "DatHang";
    public const string XemDonHangCuaMinh = "XemDonHangCuaMinh";

    public static readonly Dictionary<string, string> DisplayNames = new()
    {
        { XemDashboard, "Xem Dashboard" },
        { QuanLySanPham, "Quản lý sản phẩm" },
        { QuanLyDanhMuc, "Quản lý danh mục" },
        { QuanLyThuongHieu, "Quản lý thương hiệu" },
        { XemDonHang, "Xem đơn hàng" },
        { CapNhatTrangThaiDon, "Cập nhật trạng thái đơn" },
        { QuanLyKhuyenMai, "Quản lý khuyến mãi" },
        { XemBaoCaoDoanhThu, "Xem báo cáo doanh thu" },
        { QuanLyNguoiDung, "Quản lý người dùng" },
        { PhanQuyenVaiTro, "Phân quyền vai trò" },
        { DatHang, "Đặt hàng" },
        { XemDonHangCuaMinh, "Xem đơn hàng của mình" }
    };
}
