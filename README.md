# NexusGear — Cửa hàng phụ kiện gaming

Website thương mại điện tử bán phụ kiện gaming (chuột, bàn phím, tai nghe, ghế gaming…) được xây dựng bằng **ASP.NET Core MVC**, **Entity Framework Core** và **SQL Server**. Giao diện tối (dark theme) kết hợp hiệu ứng glassmorphism; hỗ trợ lọc theo thông số kỹ thuật, giỏ hàng, thanh toán và khu vực quản trị.

## Tính năng chính

### Dành cho khách hàng

- **Danh mục sản phẩm**: phân trang, tìm kiếm, lọc theo:
  - Danh mục, thương hiệu, khoảng giá
  - Thông số: kết nối (có dây/không dây), loại switch, DPI
- **Chi tiết sản phẩm**: mô tả, thông số, tồn kho, thêm vào giỏ
- **Giỏ hàng**:
  - Khách chưa đăng nhập: lưu theo phiên (session)
  - Đã đăng nhập: lưu cơ sở dữ liệu; tự gộp giỏ khi đăng nhập
- **Tài khoản**: đăng ký, đăng nhập (ASP.NET Core Identity)
- **Thanh toán** hai bước: nhập địa chỉ giao hàng → thanh toán khi nhận hàng (COD) hoặc thẻ mô phỏng
- **Lịch sử đơn hàng** trên tài khoản cá nhân

### Dành cho quản trị viên

- **Bảng điều khiển**: tổng doanh thu, số đơn, số khách, cảnh báo sản phẩm sắp hết hàng
- **Quản lý sản phẩm**: thêm, sửa, xóa, tải ảnh lên
- **Quản lý danh mục và thương hiệu**
- **Quản lý đơn hàng**: xem chi tiết, đổi trạng thái (Chờ xử lý → Đang giao → Hoàn thành / Đã hủy)

## Công nghệ

| Hạng mục | Công nghệ |
|----------|-----------|
| Nền tảng | ASP.NET Core MVC (.NET 10) |
| Truy cập dữ liệu | Entity Framework Core 10 (Code First) |
| Cơ sở dữ liệu | SQL Server LocalDB |
| Xác thực | ASP.NET Core Identity (Admin / Customer) |
| Giao diện | Razor Views, CSS tùy biến (font Outfit & Inter) |
| Kiến trúc | MVC + Repository Pattern |

## Yêu cầu cài đặt

- [.NET SDK 10](https://dotnet.microsoft.com/download) trở lên
- [SQL Server LocalDB](https://learn.microsoft.com/sql/database-engine/configure-windows/sql-server-express-localdb) (thường đi kèm Visual Studio)
- Công cụ dòng lệnh EF Core (khuyến nghị):

```bash
dotnet tool install --global dotnet-ef --version 10.0.8
```

## Hướng dẫn chạy dự án

### Bước 1 — Mở thư mục dự án

```bash
cd DoAnLapTrinhWeb
```

### Bước 2 — Vào thư mục ứng dụng web

```bash
cd WebApplication1\WebApplication1
```

### Bước 3 — Khôi phục gói và biên dịch

```bash
dotnet restore
dotnet build
```

### Bước 4 — Tạo hoặc cập nhật cơ sở dữ liệu

```bash
dotnet ef database update
```

> Khi chạy lần đầu, ứng dụng tự áp dụng migration và **nạp dữ liệu mẫu** (danh mục, thương hiệu, sản phẩm, tài khoản quản trị).

### Bước 5 — Chạy ứng dụng

```bash
dotnet run
```

Truy cập trên trình duyệt:

- HTTPS: `https://localhost:7169`
- HTTP: `http://localhost:5224`

## Tài khoản mặc định

| Vai trò | Email | Mật khẩu |
|---------|-------|----------|
| Quản trị viên | `admin@nexusgear.local` | `Admin@123` |

Khách hàng mới: **đăng ký** tại đường dẫn `/Account/Register` (vai trò `Customer`).

## Cấu hình kết nối cơ sở dữ liệu

Chỉnh trong file `WebApplication1/WebApplication1/appsettings.json`:

```json
"DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=NexusGearDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
```

Nếu dùng SQL Server Express hoặc instance khác, hãy sửa giá trị `Server=` cho đúng máy của bạn.

## Cấu trúc thư mục

```
DoAnLapTrinhWeb/
├── openspec/                    # Đặc tả dự án (OpenSpec)
├── WebApplication1/
│   └── WebApplication1/
│       ├── Areas/Admin/         # Khu vực quản trị
│       ├── Controllers/         # Điều khiển: Trang chủ, Sản phẩm, Giỏ, Thanh toán, Tài khoản
│       ├── Data/                # DbContext, DbSeeder
│       ├── Models/              # Thực thể & Identity
│       ├── Repositories/        # Lớp repository
│       ├── Services/            # Dịch vụ giỏ hàng
│       ├── Views/               # Giao diện cửa hàng
│       ├── wwwroot/css/         # gaming-theme.css
│       └── Migrations/          # Migration EF Core
└── README.md
```

## Luồng thao tác gợi ý

1. Vào **Cửa hàng** → lọc / tìm sản phẩm → xem chi tiết → **Thêm vào giỏ**
2. **Đăng ký** → **Đăng nhập** → **Thanh toán** (bắt buộc đã đăng nhập)
3. Đăng nhập **quản trị** → truy cập `/Admin` → quản lý sản phẩm và đơn hàng

## Lưu ý

- Thanh toán bằng thẻ chỉ **mô phỏng**, không kết nối cổng thanh toán thật.
- Phí vận chuyển cố định: **50.000 ₫** / đơn.
- Ảnh sản phẩm mẫu dùng file placeholder trong `wwwroot/images/products/`.

## Đặc tả dự án (OpenSpec)

Mô tả chi tiết nằm trong change `gaming-gear-ecommerce` thư mục `openspec/changes/`. Toàn bộ 35 hạng mục triển khai đã hoàn thành.

## Thông tin đồ án

**Đồ án lập trình web** — `DoAnLapTrinhWeb`
